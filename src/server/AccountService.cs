using Domain0.Repository;
using Domain0.Repository.Model;
using System;
using System.Security;
using System.Threading.Tasks;
using Domain0.Model;
using AutoMapper;
using Domain0.Exceptions;
using System.Linq;

namespace Domain0.Service
{
    public interface IAccountService
    {
        Task Register(decimal phone);

        Task<bool> DoesUserExists(decimal phone);

        Task<UserProfile> CreateUser(ForceCreateUserRequest request);

        Task<AccessTokenResponse> Login(SmsLoginRequest request);
    }

    public class AccountService : IAccountService
    {
        private readonly IMapper _mapper;

        private readonly ISmsClient _smsClient;

        private readonly IAuthGenerator _authGenerator;

        private readonly IAccountRepository _accountRepository;

        private readonly IRoleRepository _roleRepository;

        private readonly ISmsRequestRepository _smsRequestRepository;

        private readonly IMessageTemplateRepository _messageTemplateRepository;

        private readonly IPermissionRepository _permissionRepository;

        private readonly ITokenRegistrationRepository _tokenRegistrationRepository;

        public AccountService(
            IMapper mapper,
            ISmsClient smsClient,
            IAuthGenerator authGenerator,
            IAccountRepository accountRepository,
            IRoleRepository roleRepository,
            ISmsRequestRepository smsRequestRepository,
            IMessageTemplateRepository messageTemplateRepository,
            IPermissionRepository permissionRepository,
            ITokenRegistrationRepository tokenRegistrationRepository)
        {
            _mapper = mapper;
            _smsClient = smsClient;
            _authGenerator = authGenerator;

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

            var password = _authGenerator.GeneratePassword();
            var expiredAt = TimeSpan.FromSeconds(90);
            await _smsRequestRepository.Save(new SmsRequest
            {
                Phone = phone,
                Password = password,
                ExpiredAt = DateTime.UtcNow.Add(expiredAt)
            });

            var template = await _messageTemplateRepository.GetRegisterTemplate();
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

            var password = _authGenerator.GeneratePassword();
            var salt = _authGenerator.GenerateSalt();
            var id = await _accountRepository.Insert(new Account
            {
                Phone = phone,
                Password = _authGenerator.HashPassword(password, salt),
                Salt = salt,
                Name = request.Name
            });

            var roles = await _roleRepository.GetByIds(request.Roles.ToArray());
            if (roles.Length != (request.Roles?.Count ?? 0))
                throw new NotFoundException(nameof(request.Roles), string.Join(",",
                    request.Roles.Where(role =>
                        roles.All(r => string.Equals(r.Code, role, StringComparison.OrdinalIgnoreCase)))));

            if (roles.Length == 0)
                await _roleRepository.AddUserToDefaultRoles(id);
            else
                await _roleRepository.AddUserToRoles(id, request.Roles.ToArray());

            var result = _mapper.Map<UserProfile>(await _accountRepository.FindByLogin(phone.ToString()));
            if (request.BlockSmsSend)
                return result;

            string message;
            if (string.IsNullOrEmpty(request.CustomSmsTemplate))
                message = string.Format(await _messageTemplateRepository.GetWelcomeTemplate(),
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
                var principal = _authGenerator.Parse(accessToken);
                if (!principal.GetPermissions().All(permission => permissions.Contains(permission)))
                    accessToken = null;
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                accessToken = _authGenerator.GenerateAccessToken(account.Id, permissions);
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

            var refreshToken = _authGenerator.GenerateRefreshToken(registration.Id, account.Id);
            return new AccessTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Profile = _mapper.Map<UserProfile>(account)
            };
        }

        public async Task<AccessTokenResponse> Login(SmsLoginRequest request)
        {
            var account = await _accountRepository.FindByLogin(request.Phone);
            if (account != null)
            {
                if (_authGenerator.CheckPassword(request.Password, account.Password, account.Salt))
                    return await GetTokenResponse(account);
                else
                    return null;
            }

            // sms request registration
            var phone = account?.Phone ?? decimal.Parse(request.Phone);
            var smsRequest = await _smsRequestRepository.Pick(phone);
            if (smsRequest == null)
                return null;
            if (smsRequest.ExpiredAt < DateTime.UtcNow)
            {
                await _smsRequestRepository.Remove(phone);
                return null;
            }
            if (!string.Equals(smsRequest.Password, request.Password, StringComparison.OrdinalIgnoreCase))
                return null;

            // remove confirmed request
            await _smsRequestRepository.Remove(phone);

            // confirm sms request
            var salt = _authGenerator.GenerateSalt();
            var password = _authGenerator.HashPassword(request.Password, salt);
            if (account != null)
            {
                // change password
                account.Salt = salt;
                account.Password = password;
                await _accountRepository.Update(account);
            }
            else
            {
                // confirm registration
                var userId = await _accountRepository.Insert(account = new Account
                {
                    Name = request.Phone.ToString(),
                    Phone = phone,
                    Login = phone.ToString(),
                    Salt = salt,
                    Password = password
                });
                await _roleRepository.AddUserToDefaultRoles(userId);
            }

            return await GetTokenResponse(account);
        }
    }
}