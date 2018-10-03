using Domain0.Repository;
using Domain0.Repository.Model;
using System;
using System.Globalization;
using System.Security;
using System.Threading.Tasks;
using Domain0.Model;
using AutoMapper;
using Domain0.Exceptions;
using System.Linq;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Domain0.Service
{
    public static class ClaimsPrincipalExtensions
    {
        public static string[] GetPermissions(this ClaimsPrincipal principal)
            => principal.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).Distinct().ToArray();
    }

    public class AccountServiceSettings
    {
        public TimeSpan PinExpirationTime { get; set; }

        public TimeSpan EmailCodeExpirationTime { get; set; }
    }

    public interface IAccountService
    {
        Task Register(decimal phone);

        Task Register(string email);

        Task<bool> DoesUserExists(decimal phone);

        Task<bool> DoesUserExists(string email);

        Task<UserProfile> CreateUser(ForceCreateUserRequest request);

        Task<UserProfile> CreateUser(ForceCreateEmailUserRequest request);

        Task<AccessTokenResponse> Login(SmsLoginRequest request);

        Task<AccessTokenResponse> Login(EmailLoginRequest request);

        Task ChangePassword(ChangePasswordRequest request);

        Task RequestResetPassword(decimal phone);

        Task RequestResetPassword(string email);

        Task RequestChangePhone(ChangePhoneUserRequest changePhoneRequest);

        Task CommitChangePhone(long pin);

        Task RequestChangeEmail(ChangeEmailUserRequest changeEmailRequest);

        Task CommitChangeEmail(long pin);

        Task ForceChangePhone(ChangePhoneRequest request);

        Task ForceChangeEmail(ChangeEmailRequest request);

        Task ForceResetPassword(long phone);

        Task ForceResetPassword(string email);

        Task<AccessTokenResponse> Refresh(string refreshToken);

        Task<UserProfile> GetMyProfile();

        Task<UserProfile> GetProfileByPhone(decimal phone);

        Task<UserProfile> GetProfileByUserId(int id);

        Task<UserProfile> UpdateUser(UserProfile user);

        Task<UserProfile[]> GetProfilesByFilter(UserProfileFilter filter);
    }

    public class AccountService : IAccountService
    {
        public AccountService(
            IAccountRepository accountRepositoryInstance,
            ICultureRequestContext cultureRequestContextInstance,
            IEmailClient emailClientInstance,
            IEmailRequestRepository emailRequestRepositoryInstance,
            IMapper mapperInstance,
            IMessageTemplateRepository messageTemplateRepositoryInstance,
            IPasswordGenerator passwordGeneratorInstance,
            IPermissionRepository permissionRepositoryInstance,
            IRequestContext requestContextInstance,
            IRoleRepository roleRepositoryInstance,
            ISmsClient smsClientInstance,
            ISmsRequestRepository smsRequestRepositoryInstance,
            ITokenGenerator tokenGeneratorInstance,
            ITokenRegistrationRepository tokenRegistrationRepositoryInstance,
            TokenGeneratorSettings tokenGeneratorSettingsInstance,
            AccountServiceSettings accountServiceSettingsInstance)
        {
            accountRepository = accountRepositoryInstance;
            cultureRequestContext = cultureRequestContextInstance;
            emailClient = emailClientInstance;
            emailRequestRepository = emailRequestRepositoryInstance;
            mapper = mapperInstance;
            messageTemplateRepository = messageTemplateRepositoryInstance;
            passwordGenerator = passwordGeneratorInstance;
            permissionRepository = permissionRepositoryInstance;
            requestContext = requestContextInstance;
            roleRepository = roleRepositoryInstance;
            smsClient = smsClientInstance;
            smsRequestRepository = smsRequestRepositoryInstance;
            tokenGenerator = tokenGeneratorInstance;
            tokenRegistrationRepository = tokenRegistrationRepositoryInstance;
            tokenGeneratorSettings = tokenGeneratorSettingsInstance;
            accountServiceSettings = accountServiceSettingsInstance;
        }

        public async Task Register(decimal phone)
        {
            if (await DoesUserExists(phone))
                throw new SecurityException("user exists");

            var existed = await smsRequestRepository.Pick(phone);
            if (existed != null)
                return;

            var password = passwordGenerator.GeneratePassword();
            var expiredAt = accountServiceSettings.PinExpirationTime;
            await smsRequestRepository.Save(new SmsRequest
            {
                Phone = phone,
                Password = password,
                ExpiredAt = DateTime.UtcNow.Add(expiredAt)
            });

            var template = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RegisterTemplate,
                cultureRequestContext.Culture, 
                MessageTemplateType.sms);
            var message = string.Format(template, password, expiredAt.TotalMinutes);

            await smsClient.Send(phone, message);
        }

        public async Task Register(string email)
        {
            if (await DoesUserExists(email))
                throw new SecurityException("user exists");

            var existed = await emailRequestRepository.Pick(email);
            if (existed != null)
                return;

            var password = passwordGenerator.GeneratePassword();
            var expiredAt = accountServiceSettings.PinExpirationTime;
            await emailRequestRepository.Save(new EmailRequest
            {
                Email = email,
                Password = password,
                ExpiredAt = DateTime.UtcNow.Add(expiredAt)
            });

            var subjectTemplate = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RegisterSubjectTemplate,
                cultureRequestContext.Culture, 
                MessageTemplateType.email);
            var template = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RegisterTemplate,
                cultureRequestContext.Culture, 
                MessageTemplateType.email);

            var message = string.Format(template, password, expiredAt.TotalMinutes);
            var subject = string.Format(subjectTemplate, email, "domain0");

            await emailClient.Send(subject, email, message);
        }

        public async Task<bool> DoesUserExists(decimal phone)
        {
            var account = await accountRepository.FindByPhone(phone);
            return account != null;
        }

        public async Task<bool> DoesUserExists(string email)
        {
            var account = await accountRepository.FindByLogin(email);
            return account != null;
        }


        public async Task<UserProfile> CreateUser(ForceCreateUserRequest request)
        {
            if (!request.Phone.HasValue)
                throw new ArgumentException(nameof(ForceCreateUserRequest.Phone));

            var phone = request.Phone.Value;
            if (await DoesUserExists(phone))
                throw new SecurityException("user exists");

            var password = passwordGenerator.GeneratePassword();
            var id = await accountRepository.Insert(new Account
            {
                Phone = phone,
                Password = passwordGenerator.HashPassword(password),
                Name = request.Name
            });

            var roles = await roleRepository.GetByRoleNames(request.Roles.ToArray());
            if (roles.Length != request.Roles?.Count)
                throw new NotFoundException(nameof(request.Roles), string.Join(",",
                    request.Roles.Where(role =>
                        roles.All(r => string.Equals(r.Name, role, StringComparison.OrdinalIgnoreCase)))));

            if (roles.Length == 0)
                await roleRepository.AddUserToDefaultRoles(id);
            else
                await roleRepository.AddUserToRoles(id, request.Roles.ToArray());

            var result = mapper.Map<UserProfile>(await accountRepository.FindByLogin(phone.ToString()));
            if (request.BlockSmsSend)
                return result;

            string message;
            if (string.IsNullOrEmpty(request.CustomSmsTemplate))
                message = string.Format(await messageTemplateRepository.GetTemplate(
                    MessageTemplateName.WelcomeTemplate,
                    cultureRequestContext.Culture, 
                    MessageTemplateType.sms),
                    request.Phone, password);
            else
                message = request.CustomSmsTemplate
                    .Replace("{phone}", request.Phone.ToString())
                    .Replace("{password}", password);

            await smsClient.Send(request.Phone.Value, message);

            return result;
        }

        public async Task<UserProfile> CreateUser(ForceCreateEmailUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException(nameof(ForceCreateEmailUserRequest.Email));

            var email = request.Email;
            if (await DoesUserExists(email))
                throw new SecurityException("user exists");

            var password = passwordGenerator.GeneratePassword();
            var id = await accountRepository.Insert(new Account
            {
                Email = email,
                Login = email,
                Password = passwordGenerator.HashPassword(password),
                Name = request.Name
            });

            var roles = await roleRepository.GetByRoleNames(request.Roles.ToArray());
            if (roles.Length != request.Roles?.Count)
                throw new NotFoundException(nameof(request.Roles), string.Join(",",
                    request.Roles.Where(role =>
                        roles.All(r => string.Equals(r.Name, role, StringComparison.OrdinalIgnoreCase)))));

            if (roles.Length == 0)
                await roleRepository.AddUserToDefaultRoles(id);
            else
                await roleRepository.AddUserToRoles(id, request.Roles.ToArray());

            var result = mapper.Map<UserProfile>(await accountRepository.FindByLogin(email));
            if (request.BlockSmsSend)
                return result;

            string message;
            string subject;
            if (string.IsNullOrEmpty(request.CustomEmailTemplate)
                || string.IsNullOrEmpty(request.CustomEmailSubjectTemplate))
            {
                subject = await messageTemplateRepository.GetTemplate(
                    MessageTemplateName.WelcomeSubjectTemplate,
                    cultureRequestContext.Culture,
                    MessageTemplateType.email);

                message = string.Format(await messageTemplateRepository.GetTemplate(
                        MessageTemplateName.WelcomeTemplate,
                        cultureRequestContext.Culture,
                        MessageTemplateType.email),
                    request.Email, password);
            }
            else
            {
                subject = request.CustomEmailSubjectTemplate;
                message = request.CustomEmailTemplate
                    .Replace("{email}", request.Email)
                    .Replace("{password}", password);
            }

            await emailClient.Send(subject, request.Email, message);

            return result;
        }

        public async Task<AccessTokenResponse> GetTokenResponse(Account account)
        {
            var userPermissions = await permissionRepository.GetByUserId(account.Id);
            var registration = await tokenRegistrationRepository.FindLastTokenByUserId(account.Id);
            string accessToken = registration?.AccessToken;
            if (!string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    // if permission changed we should make new token
                    var principal = tokenGenerator.Parse(accessToken);
                    if (principal == null
                        || IsRightsDifferent(userPermissions, principal.GetPermissions()))
                        accessToken = null;
                }
                catch (SecurityTokenValidationException)
                {
                    // if token expired or some sensitive properties changes we should make new token
                    accessToken = null;
                }
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                var issueDate = DateTime.UtcNow;
                var expiredAt = issueDate.Add(tokenGeneratorSettings.Lifetime);
                accessToken = tokenGenerator.GenerateAccessToken(
                    account.Id, 
                    issueDate, 
                    userPermissions.Select(p => p.Name).ToArray());
                await tokenRegistrationRepository.Save(registration = new TokenRegistration
                {
                    UserId = account.Id,
                    IssuedAt = issueDate,
                    AccessToken = accessToken,
                    ExpiredAt = expiredAt
                });
            }
            else
                accessToken = registration.AccessToken;

            var refreshToken = tokenGenerator.GenerateRefreshToken(registration.Id, account.Id);
            return new AccessTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Profile = mapper.Map<UserProfile>(account)
            };
        }

        private static bool IsRightsDifferent(
            Repository.Model.Permission[] userPermissions, 
            string[] tokenPermissions)
        {
            // assume checking permissions is distinctive
            if (userPermissions.Length != tokenPermissions.Length)
                // rigths are different!
                return true;

                var matchedRights = userPermissions.Select(p => p.Name)
                .Join(tokenPermissions,
                    up => up,
                    tp => tp,
                    (up, tp) => up)
                .ToArray();

            if (userPermissions.Length != matchedRights.Length)
                // rigths are different!
                return true;

            return false;
        }

        public async Task<AccessTokenResponse> Login(SmsLoginRequest request)
        {
            var phone = decimal.Parse(request.Phone);
            var hashPassword = passwordGenerator.HashPassword(request.Password);

            // login account
            var account = await accountRepository.FindByLogin(request.Phone);
            if (account != null && passwordGenerator.CheckPassword(request.Password, account.Password))
                return await GetTokenResponse(account);

            // remove try confirm registration
            if (!await smsRequestRepository.ConfirmRegister(phone, request.Password))
                return null;

            // confirm sms request
            if (account != null)
            {
                // change password
                account.Password = hashPassword;
                await accountRepository.Update(account);
            }
            else
            {
                // confirm registration
                var userId = await accountRepository.Insert(account = new Account
                {
                    Name = phone.ToString(CultureInfo.InvariantCulture),
                    Phone = phone,
                    Login = phone.ToString(CultureInfo.InvariantCulture),
                    Password = hashPassword
                });

                // store new assigned Id
                account.Id = userId;

                await roleRepository.AddUserToDefaultRoles(userId);
            }

            return await GetTokenResponse(account);
        }

        public async Task<AccessTokenResponse> Login(EmailLoginRequest request)
        {
            var email = request.Email;
            var hashPassword = passwordGenerator.HashPassword(request.Password);

            // login account
            var account = await accountRepository.FindByLogin(email);
            if (account != null && passwordGenerator.CheckPassword(request.Password, account.Password))
                return await GetTokenResponse(account);

            // remove try confirm registration
            if (!await emailRequestRepository.ConfirmRegister(email, request.Password))
                return null;

            // confirm email request
            if (account != null)
            {
                // change password
                account.Password = hashPassword;
                await accountRepository.Update(account);
            }
            else
            {
                // confirm registration
                var userId = await accountRepository.Insert(account = new Account
                {
                    Name = email,
                    Email = email,
                    Login = email,
                    Password = hashPassword
                });

                // store new assigned Id
                account.Id = userId;

                await roleRepository.AddUserToDefaultRoles(userId);
            }

            return await GetTokenResponse(account);
        }

        public async Task ChangePassword(ChangePasswordRequest request)
        {
            var account = await accountRepository.FindByUserId(requestContext.UserId);
            if (account == null)
                throw new NotFoundException(nameof(IRequestContext.UserId), "account not found");

            if (!passwordGenerator.CheckPassword(request.OldPassword, account.Password))
                throw new SecurityException("password not match");

            account.Password = passwordGenerator.HashPassword(request.NewPassword);
            await accountRepository.Update(account);
        }

        public async Task RequestResetPassword(decimal phone)
        {
            var account = await accountRepository.FindByPhone(phone);
            if (account == null)
                throw new NotFoundException(nameof(phone), "account not found");

            var password = passwordGenerator.GeneratePassword();
            var expiredAt = accountServiceSettings.PinExpirationTime;
            await smsRequestRepository.Save(new SmsRequest
            {
                Phone = phone,
                Password = password,
                ExpiredAt = DateTime.UtcNow.Add(expiredAt)
            });

            var template = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RequestResetTemplate,
                cultureRequestContext.Culture,
                MessageTemplateType.sms);

            var message = string.Format(template, password, expiredAt.TotalMinutes);
            await smsClient.Send(phone, message);
        }

        public async Task RequestResetPassword(string email)
        {
            var account = await accountRepository.FindByLogin(email);
            if (account == null)
                throw new NotFoundException(nameof(email), "account not found");

            var password = passwordGenerator.GeneratePassword();
            var expiredAt = accountServiceSettings.PinExpirationTime;
            await emailRequestRepository.Save(new EmailRequest
            {
                Email = email,
                Password = password,
                ExpiredAt = DateTime.UtcNow.Add(expiredAt)
            });


            var subjectTemplate = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RequestResetSubjectTemplate,
                cultureRequestContext.Culture, 
                MessageTemplateType.email);

            var template = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RequestResetTemplate,
                cultureRequestContext.Culture, 
                MessageTemplateType.email);

            var message = string.Format(template, password, expiredAt.TotalMinutes);
            var subject = string.Format(subjectTemplate, "domain0", account.Name);

            await emailClient.Send(subject, email, message);
        }

        public async Task RequestChangePhone(ChangePhoneUserRequest changePhoneRequest)
        {
            var userId = requestContext.UserId;

            var account = await accountRepository.FindByUserId(userId);
            if (account == null)
                throw new NotFoundException(nameof(account), "account not found");

            if (!passwordGenerator.CheckPassword(changePhoneRequest.Password, account.Password))
                throw new SecurityException("password not match");

            var pin = passwordGenerator.GeneratePassword();
            var expiredAt = accountServiceSettings.PinExpirationTime;
            await smsRequestRepository.Save(new SmsRequest
            {
                UserId = userId,
                Phone = changePhoneRequest.Phone,
                Password = pin,
                ExpiredAt = DateTime.UtcNow.Add(expiredAt)
            });

            var template = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RequestPhoneChangeTemplate,
                cultureRequestContext.Culture,
                MessageTemplateType.sms);

            var message = string.Format(template, pin, expiredAt.TotalMinutes);
            await smsClient.Send(changePhoneRequest.Phone, message);
        }

        public async Task CommitChangePhone(long pin)
        {
            var userId = requestContext.UserId;

            var account = await accountRepository.FindByUserId(userId);
            if (account == null)
                throw new NotFoundException(nameof(account), "account not found");

            var smsRequest = await smsRequestRepository.PickByUserId(userId);

            if (pin.ToString() != smsRequest.Password)
                throw new SecurityException("wrong pin");

            account.Phone = smsRequest.Phone;
            account.Login = smsRequest.Phone.ToString(CultureInfo.InvariantCulture);
            await accountRepository.Update(account);
        }

        public async Task RequestChangeEmail(ChangeEmailUserRequest changeEmailRequest)
        {
            var userId = requestContext.UserId;

            var account = await accountRepository.FindByUserId(userId);
            if (account == null)
                throw new NotFoundException(nameof(account), "account not found");

            if (!passwordGenerator.CheckPassword(changeEmailRequest.Password, account.Password))
                throw new SecurityException("password not match");

            var pin = passwordGenerator.GeneratePassword();
            var expiredAt = accountServiceSettings.EmailCodeExpirationTime;
            await emailRequestRepository.Save(new EmailRequest
            {
                UserId = userId,
                Email = changeEmailRequest.Email,
                Password = pin,
                ExpiredAt = DateTime.UtcNow.Add(expiredAt)
            });

            var template = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RequestEmailChangeTemplate,
                cultureRequestContext.Culture,
                MessageTemplateType.email);

            var subject = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RequestEmailChangeSubjectTemplate,
                cultureRequestContext.Culture,
                MessageTemplateType.email);

            var message = string.Format(template, pin, expiredAt.TotalMinutes);
            await emailClient.Send(subject, changeEmailRequest.Email, message);
        }

        public async Task CommitChangeEmail(long pin)
        {
            var userId = requestContext.UserId;

            var account = await accountRepository.FindByUserId(userId);
            if (account == null)
                throw new NotFoundException(nameof(account), "account not found");

            var emailRequest = await emailRequestRepository.PickByUserId(userId);

            if (pin.ToString() != emailRequest.Password)
                throw new SecurityException("wrong pin");

            account.Login = emailRequest.Email;
            account.Email = emailRequest.Email;
            await accountRepository.Update(account);
        }


        public async Task ForceChangePhone(ChangePhoneRequest request)
        {
            var account = await accountRepository.FindByUserId(request.UserId);
            if (account == null)
                throw new NotFoundException(nameof(request.UserId), "account not found");

            if (account.Login == account.Phone.ToString())
                account.Login = request.NewPhone.ToString();
            account.Phone = request.NewPhone;

            await accountRepository.Update(account);
        }

        public async Task ForceChangeEmail(ChangeEmailRequest request)
        {
            var account = await accountRepository.FindByUserId(request.UserId);
            if (account == null)
                throw new NotFoundException(nameof(request.UserId), "account not found");

            if (account.Login == account.Email)
                account.Login = request.NewEmail;
            account.Email = request.NewEmail;

            await accountRepository.Update(account);
        }

        public async Task ForceResetPassword(long phone)
        {
            var account = await accountRepository.FindByPhone(phone);
            if (account == null)
                throw new NotFoundException(nameof(phone), "account not found");

            var newPassword = passwordGenerator.GeneratePassword();
            var hashNewPassword = passwordGenerator.HashPassword(newPassword);

            // change password
            account.Password = hashNewPassword;
            await accountRepository.Update(account);

            var template = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.ForcePasswordResetTemplate,
                cultureRequestContext.Culture,
                MessageTemplateType.sms);

            var message = string.Format(template, newPassword);
            await smsClient.Send(phone, message);
        }

        public async Task ForceResetPassword(string email)
        {
            var account = await accountRepository.FindByLogin(email);
            if (account == null)
                throw new NotFoundException(nameof(email), "account not found");

            var newPassword = passwordGenerator.GeneratePassword();
            var hashNewPassword = passwordGenerator.HashPassword(newPassword);

            // change password
            account.Password = hashNewPassword;
            await accountRepository.Update(account);

            var template = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.ForcePasswordResetTemplate,
                cultureRequestContext.Culture,
                MessageTemplateType.email);

            var subjectTemplate = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.ForcePasswordResetSubjectTemplate,
                cultureRequestContext.Culture,
                MessageTemplateType.email);

            var subject = string.Format(subjectTemplate, email, "domain0");
            var message = string.Format(template, newPassword);
            await emailClient.Send(subject, email, message);
        }

        public async Task<AccessTokenResponse> Refresh(string refreshToken)
        {
            var id = tokenGenerator.GetTid(refreshToken);
            var tokenRegistry = await tokenRegistrationRepository.FindById(id);
            if (tokenRegistry == null)
                throw new NotFoundException(nameof(tokenRegistry), id);
            var account = await accountRepository.FindByUserId(tokenRegistry.UserId);
            if (account == null)
                throw new NotFoundException(nameof(account), tokenRegistry.UserId);

            var principal = tokenGenerator.Parse(tokenRegistry.AccessToken);
            var accessToken = tokenGenerator.GenerateAccessToken(account.Id, principal.GetPermissions());

            return new AccessTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Profile = mapper.Map<UserProfile>(account)
            };
        }

        public async Task<UserProfile> GetMyProfile()
        {
            var userId = requestContext.UserId;
            var account = await accountRepository.FindByUserId(userId);
            if (account == null)
                throw new NotFoundException(nameof(account), userId);
            return mapper.Map<UserProfile>(account);
        }

        public async Task<UserProfile> GetProfileByPhone(decimal phone)
        {
            var account = await accountRepository.FindByPhone(phone);
            if (account == null)
                throw new NotFoundException(nameof(account), phone);
            return mapper.Map<UserProfile>(account);
        }

        public async Task<UserProfile> GetProfileByUserId(int id)
        {
            var account = await accountRepository.FindByUserId(id);
            if (account == null)
                throw new NotFoundException(nameof(account), id);
            return mapper.Map<UserProfile>(account);
        }

        public async Task<UserProfile[]> GetProfilesByFilter(UserProfileFilter filter)
        {
            var accounts = await accountRepository.FindByUserIds(filter.UserIds);
            return mapper.Map<UserProfile[]>(accounts);
        }

        public async Task<UserProfile> UpdateUser(UserProfile user)
        {
            var account = mapper.Map<Account>(user);

            await accountRepository.Update(account);

            var updatedAccount = await accountRepository.FindByUserId(account.Id);

            return mapper.Map<UserProfile>(updatedAccount);
        }

        private readonly IEmailClient emailClient;

        private readonly IEmailRequestRepository emailRequestRepository;

        private readonly IMapper mapper;

        private readonly ISmsClient smsClient;

        private readonly IPasswordGenerator passwordGenerator;

        private readonly ITokenGenerator tokenGenerator;

        private readonly IRequestContext requestContext;

        private readonly IAccountRepository accountRepository;

        private readonly ICultureRequestContext cultureRequestContext;

        private readonly IRoleRepository roleRepository;

        private readonly ISmsRequestRepository smsRequestRepository;

        private readonly IMessageTemplateRepository messageTemplateRepository;

        private readonly IPermissionRepository permissionRepository;

        private readonly ITokenRegistrationRepository tokenRegistrationRepository;

        private readonly TokenGeneratorSettings tokenGeneratorSettings;

        private readonly AccountServiceSettings accountServiceSettings;
    }
}