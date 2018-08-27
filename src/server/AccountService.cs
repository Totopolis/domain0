using Domain0.Repository;
using Domain0.Repository.Model;
using System;
using System.Security;
using System.Threading.Tasks;
using Domain0.Model;
using AutoMapper;
using Domain0.Exceptions;
using System.Linq;
using System.Security.Claims;

namespace Domain0.Service
{
    public static class ClaimsPrincipalExtensions
    {
        public static string[] GetPermissions(this ClaimsPrincipal principal)
            => principal.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).Distinct().ToArray();
    }

    public interface IAccountService
    {
        Task Register(decimal phone);

        Task<bool> DoesUserExists(decimal phone);

        Task<UserProfile> CreateUser(ForceCreateUserRequest request);

        Task<AccessTokenResponse> Login(SmsLoginRequest request);

        Task ChangePassword(ChangePasswordRequest request);

        Task RequestResetPassword(decimal phone);

        Task ForceChangePhone(ChangePhoneRequest request);

        Task<AccessTokenResponse> Refresh(string refreshToken);

        Task<UserProfile> GetMyProfile();

        Task<UserProfile> GetProfileByPhone(decimal phone);

        Task<UserProfile> GetProfileByUserId(int id);

        Task<UserProfile[]> GetProfilesByFilter(UserProfileFilter filter);
    }

    public class AccountService : IAccountService
    {
        private readonly IMapper _mapper;

        private readonly ISmsClient _smsClient;

        private readonly IPasswordGenerator _passwordGenerator;

        private readonly ITokenGenerator _tokenGenerator;

        private readonly IRequestContext _requestContext;

        private readonly IAccountRepository _accountRepository;

        private readonly IRoleRepository _roleRepository;

        private readonly ISmsRequestRepository _smsRequestRepository;

        private readonly IMessageTemplateRepository _messageTemplateRepository;

        private readonly IPermissionRepository _permissionRepository;

        private readonly ITokenRegistrationRepository _tokenRegistrationRepository;

        public AccountService(
            IMapper mapper,
            ISmsClient smsClient,
            IPasswordGenerator passwordGenerator,
            ITokenGenerator tokenGenerator,
            IRequestContext requestContext,
            IAccountRepository accountRepository,
            IRoleRepository roleRepository,
            ISmsRequestRepository smsRequestRepository,
            IMessageTemplateRepository messageTemplateRepository,
            IPermissionRepository permissionRepository,
            ITokenRegistrationRepository tokenRegistrationRepository)
        {
            _mapper = mapper;
            _smsClient = smsClient;
            _passwordGenerator = passwordGenerator;
            _tokenGenerator = tokenGenerator;
            _requestContext = requestContext;

            _accountRepository = accountRepository;
            _roleRepository = roleRepository;
            _smsRequestRepository = smsRequestRepository;
            _messageTemplateRepository = messageTemplateRepository;
            _permissionRepository = permissionRepository;
            _tokenRegistrationRepository = tokenRegistrationRepository;
        }

        public async Task Register(decimal phone)
        {
            if (await DoesUserExists(phone))
                throw new SecurityException("user exists");

            var existed = await _smsRequestRepository.Pick(phone);
            if (existed != null)
                return;

            var password = _passwordGenerator.GeneratePassword();
            var expiredAt = TimeSpan.FromSeconds(90);
            await _smsRequestRepository.Save(new SmsRequest
            {
                Phone = phone,
                Password = password,
                ExpiredAt = DateTime.UtcNow.Add(expiredAt)
            });

            var template = await _messageTemplateRepository.GetRegisterTemplate(MessageTemplateLocale.rus, MessageTemplateType.sms);
            var message = string.Format(template, password, expiredAt.TotalMinutes);

            await _smsClient.Send(phone, message);
        }

        public async Task<bool> DoesUserExists(decimal phone)
        {
            var account = await _accountRepository.FindByPhone(phone);
            return account != null;
        }

        public async Task<UserProfile> CreateUser(ForceCreateUserRequest request)
        {
            if (!request.Phone.HasValue)
                throw new SecurityException("user exists");

            var phone = request.Phone.Value;
            if (await DoesUserExists(phone))
                throw new SecurityException("user exists");

            var password = _passwordGenerator.GeneratePassword();
            var id = await _accountRepository.Insert(new Account
            {
                Phone = phone,
                Password = _passwordGenerator.HashPassword(password),
                Name = request.Name
            });

            var roles = await _roleRepository.GetByIds(request.Roles.ToArray());
            if (roles.Length != (request.Roles?.Count ?? 0))
                throw new NotFoundException(nameof(request.Roles), string.Join(",",
                    request.Roles.Where(role =>
                        roles.All(r => string.Equals(r.Name, role, StringComparison.OrdinalIgnoreCase)))));

            if (roles.Length == 0)
                await _roleRepository.AddUserToDefaultRoles(id);
            else
                await _roleRepository.AddUserToRoles(id, request.Roles.ToArray());

            var result = _mapper.Map<UserProfile>(await _accountRepository.FindByLogin(phone.ToString()));
            if (request.BlockSmsSend)
                return result;

            string message;
            if (string.IsNullOrEmpty(request.CustomSmsTemplate))
                message = string.Format(await _messageTemplateRepository.GetWelcomeTemplate(MessageTemplateLocale.rus, MessageTemplateType.sms),
                    request.Phone, password);
            else
                message = request.CustomSmsTemplate
                    .Replace("{phone}", request.Phone.ToString())
                    .Replace("{password}", password);

            await _smsClient.Send(request.Phone.Value, message);

            return result;
        }

        public async Task<AccessTokenResponse> GetTokenResponse(Account account)
        {
            var permissions = await _permissionRepository.GetByUserId(account.Id);
            var registration = await _tokenRegistrationRepository.FindLastTokenByUserId(account.Id);
            string accessToken = registration?.AccessToken;
            if (!string.IsNullOrEmpty(accessToken))
            {
                var principal = _tokenGenerator.Parse(accessToken);
                if (!principal?.GetPermissions().All(permission => permissions.Contains(permission)) ?? true)
                    accessToken = null;
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                accessToken = _tokenGenerator.GenerateAccessToken(account.Id, permissions);
                await _tokenRegistrationRepository.Save(registration = new TokenRegistration
                {
                    UserId = account.Id,
                    IssuedAt = DateTime.UtcNow,
                    AccessToken = accessToken,
                    ExpiredAt = DateTime.UtcNow.Add(TimeSpan.FromDays(10))
                });
            }
            else
                accessToken = registration.AccessToken;

            var refreshToken = _tokenGenerator.GenerateRefreshToken(registration.Id, account.Id);
            return new AccessTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Profile = _mapper.Map<UserProfile>(account)
            };
        }

        public async Task<AccessTokenResponse> Login(SmsLoginRequest request)
        {
            var phone = decimal.Parse(request.Phone);
            var hashPassword = _passwordGenerator.HashPassword(request.Password);

            // login account
            var account = await _accountRepository.FindByLogin(request.Phone);
            if (account != null && _passwordGenerator.CheckPassword(request.Password, account.Password))
                return await GetTokenResponse(account);

            // remove try confirm registration
            if (!await _smsRequestRepository.ConfirmRegister(phone, request.Password))
                return null;

            // confirm sms request
            if (account != null)
            {
                // change password
                account.Password = hashPassword;
                await _accountRepository.Update(account);
            }
            else
            {
                // confirm registration
                var userId = await _accountRepository.Insert(account = new Account
                {
                    Name = phone.ToString(),
                    Phone = phone,
                    Login = phone.ToString(),
                    Password = hashPassword
                });

                // store new assigned Id
                account.Id = userId;

                await _roleRepository.AddUserToDefaultRoles(userId);
            }

            return await GetTokenResponse(account);
        }

        public async Task ChangePassword(ChangePasswordRequest request)
        {
            var account = await _accountRepository.FindByUserId(_requestContext.UserId);
            if (!_passwordGenerator.CheckPassword(request.OldPassword, account?.Password))
                throw new SecurityException("password not match");

            account.Password = _passwordGenerator.HashPassword(request.NewPassword);
            await _accountRepository.Update(account);
        }

        public async Task RequestResetPassword(decimal phone)
        {
            var account = await _accountRepository.FindByPhone(phone);
            if (account == null)
                throw new NotFoundException(nameof(phone), "account not found");

            var password = _passwordGenerator.GeneratePassword();
            var expiredAt = TimeSpan.FromSeconds(90);
            await _smsRequestRepository.Save(new SmsRequest
            {
                Phone = phone,
                Password = password,
                ExpiredAt = DateTime.UtcNow.Add(expiredAt)
            });

            var template = await _messageTemplateRepository.GetRequestResetTemplate(MessageTemplateLocale.rus, MessageTemplateType.sms);
            var message = string.Format(template, password, expiredAt.TotalMinutes);

            await _smsClient.Send(phone, message);
        }

        public async Task ForceChangePhone(ChangePhoneRequest request)
        {
            var account = await _accountRepository.FindByUserId(request.UserId);
            if (account == null)
                throw new NotFoundException(nameof(request.UserId), "account not found");

            if (account.Login == account.Phone.ToString())
                account.Login = request.NewPhone.ToString();
            account.Phone = request.NewPhone;

            await _accountRepository.Update(account);
        }

        public async Task<AccessTokenResponse> Refresh(string refreshToken)
        {
            var id = _tokenGenerator.GetTid(refreshToken);
            var tokenRegistry = await _tokenRegistrationRepository.FindById(id);
            if (tokenRegistry == null)
                throw new NotFoundException(nameof(tokenRegistry), id);
            var account = await _accountRepository.FindByUserId(tokenRegistry.UserId);
            if (account == null)
                throw new NotFoundException(nameof(account), tokenRegistry.UserId);

            var principal = _tokenGenerator.Parse(tokenRegistry.AccessToken);
            var accessToken = _tokenGenerator.GenerateAccessToken(account.Id, principal.GetPermissions());

            return new AccessTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Profile = _mapper.Map<UserProfile>(account)
            };
        }

        public async Task<UserProfile> GetMyProfile()
        {
            var userId = _requestContext.UserId;
            var account = await _accountRepository.FindByUserId(userId);
            if (account == null)
                throw new NotFoundException(nameof(account), userId);
            return _mapper.Map<UserProfile>(account);
        }

        public async Task<UserProfile> GetProfileByPhone(decimal phone)
        {
            var account = await _accountRepository.FindByPhone(phone);
            if (account == null)
                throw new NotFoundException(nameof(account), phone);
            return _mapper.Map<UserProfile>(account);
        }

        public async Task<UserProfile> GetProfileByUserId(int id)
        {
            var account = await _accountRepository.FindByUserId(id);
            if (account == null)
                throw new NotFoundException(nameof(account), id);
            return _mapper.Map<UserProfile>(account);
        }

        public async Task<UserProfile[]> GetProfilesByFilter(UserProfileFilter filter)
        {
            var accounts = await _accountRepository.FindByUserIds(filter.UserIds);
            return _mapper.Map<UserProfile[]>(accounts);
        }
    }
}