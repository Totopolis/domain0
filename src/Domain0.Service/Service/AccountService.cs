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
using Domain0.Nancy.Model;
using Domain0.Nancy.Service.Ldap;
using Domain0.Tokens;
using NLog;

namespace Domain0.Service
{
    public static class ClaimsPrincipalExtensions
    {
        public static string[] GetPermissions(this ClaimsPrincipal principal)
            => principal.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).Distinct().ToArray();
    }

    public interface IAccountService
    {
        Task Register(decimal phone, string environmentToken = null);

        Task Register(string email, string environmentToken = null);

        Task<bool> DoesUserExists(decimal phone);

        Task<bool> DoesUserExists(string email);

        Task<UserProfile> CreateUser(ForceCreateUserRequest request);

        Task<UserProfile> CreateUser(ForceCreateEmailUserRequest request);

        Task<AccessTokenResponse> Login(SmsLoginRequest request);

        Task<AccessTokenResponse> Login(EmailLoginRequest request);

        Task<AccessTokenResponse> Login(ActiveDirectoryUserLoginRequest request , string environmentToken);

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

        Task ForceResetPassword(ForceResetPasswordRequest request);

        Task<AccessTokenResponse> Refresh(string prevRefreshToken);

        Task<UserProfile> GetMyProfile();

        Task<UserProfile> GetProfileByPhone(decimal phone);

        Task<UserProfile> GetProfileByUserId(int id);

        Task<UserProfile> UpdateUser(UserProfile user);

        Task DeleteUser(int id);

        Task LockUser(int id);

        Task UnlockUser(int id);

        Task<UserProfile[]> GetProfilesByFilter(UserProfileFilter filter);
    }

    public class AccountService : IAccountService
    {
        public AccountService(
            IAccountRepository accountRepositoryInstance,
            ICultureRequestContext cultureRequestContextInstance,
            IEmailClient emailClientInstance,
            IEmailRequestRepository emailRequestRepositoryInstance,
            IEnvironmentRequestContext environmentRequestContextInstance,
            ILogger loggerInstance,
            IMapper mapperInstance,
            IMessageBuilder messageBuilderInstance,
            IPasswordGenerator passwordGeneratorInstance,
            IPermissionRepository permissionRepositoryInstance,
            IRequestContext requestContextInstance,
            IRoleRepository roleRepositoryInstance,
            ISmsClient smsClientInstance,
            ISmsRequestRepository smsRequestRepositoryInstance,
            ITokenGenerator tokenGeneratorInstance,
            ITokenRegistrationRepository tokenRegistrationRepositoryInstance,
            TokenGeneratorSettings tokenGeneratorSettingsInstance,
            AccountServiceSettings accountServiceSettingsInstance,
            ILdapClient ldapClientInstance)
        {
            accountRepository = accountRepositoryInstance;
            cultureRequestContext = cultureRequestContextInstance;
            emailClient = emailClientInstance;
            emailRequestRepository = emailRequestRepositoryInstance;
            environmentRequestContext = environmentRequestContextInstance;
            logger = loggerInstance;
            mapper = mapperInstance;
            messageBuilder = messageBuilderInstance;
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
            ldapClient = ldapClientInstance;
        }

        public async Task Register(decimal phone, string environmentToken = null)
        {
            if (await DoesUserExists(phone))
            {
                logger.Warn($"Attempt to register an existing user! Phone: {phone}");
                throw new SecurityException("user exists");
            }

            var existed = await smsRequestRepository.Pick(phone);
            if (existed != null && IsNeedCooldown(existed.ExpiredAt, accountServiceSettings.PinExpirationTime))
            {
                logger.Warn($"Attempt to get pin multiple times! Phone: {phone}");
                return;
            }

            var environment = await environmentRequestContext.LoadEnvironment(environmentToken);
            CheckEnvironmentTokenValid(environmentToken, environment);

            var expirationTime = accountServiceSettings.PinExpirationTime;
            var password = passwordGenerator.GeneratePassword();
            await smsRequestRepository.Save(new SmsRequest
            {
                Phone = phone,
                Password = password,
                ExpiredAt = DateTime.UtcNow.Add(expirationTime),
                EnvironmentId = environment?.Id
            });
            logger.Info($"New user registration request. Phone: {phone}");


            var message = await messageBuilder.Build(
                MessageTemplateName.RegisterTemplate,
                MessageTemplateType.sms,
                password, expirationTime.TotalMinutes);

            await smsClient.Send(phone, message, environment?.Token);
            logger.Info($"New user pin has been sent to phone: {phone}");
        }

        public async Task Register(string email, string environmentToken = null)
        {
            if (await DoesUserExists(email))
            {
                logger.Warn($"Attempt to register an existing user! Email: {email}");
                throw new SecurityException("user exists");
            }

            var existed = await emailRequestRepository.Pick(email);
            if (existed != null && IsNeedCooldown(existed.ExpiredAt, accountServiceSettings.EmailCodeExpirationTime))
            {
                logger.Warn($"Attempt to get pin multiple times! Email: {email}");
                return;
            }

            var environment = await environmentRequestContext.LoadEnvironment(environmentToken);
            CheckEnvironmentTokenValid(environmentToken, environment);

            var password = passwordGenerator.GeneratePassword();
            var expirationTime = accountServiceSettings.EmailCodeExpirationTime;
            await emailRequestRepository.Save(new EmailRequest
            {
                Email = email,
                Password = password,
                ExpiredAt = DateTime.UtcNow.Add(expirationTime),
                EnvironmentId = environment?.Id
            });
            logger.Info($"New user registration request. Email: {email}");


            var message = await messageBuilder.Build(
                MessageTemplateName.RegisterTemplate,
                MessageTemplateType.email,
                password, expirationTime.TotalMinutes);

            var subject = await messageBuilder.Build(
                MessageTemplateName.RegisterSubjectTemplate,
                MessageTemplateType.email,
                email, "domain0");

            await emailClient.Send(subject, email, message);
            logger.Info($"New user pin has been sent to Email: {email}");

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
            logger.Info($"User width id: { requestContext.UserId } execute force create user with phone: {request.Phone}");
            if (!request.Phone.HasValue)
                throw new ArgumentException("can't be null", nameof(ForceCreateUserRequest.Phone));

            var phone = request.Phone.Value;
            if (await DoesUserExists(phone))
            {
                logger.Warn($"Attempt to register an existing user! phone: {request.Phone}");
                throw new SecurityException("user exists");
            }

            if (!string.IsNullOrWhiteSpace(request.Locale))
            {
                cultureRequestContext.Culture = CultureInfo.GetCultureInfo(request.Locale);
            }

            var environment = await environmentRequestContext.LoadEnvironment(request.EnvironmentToken);
            CheckEnvironmentTokenValid(request.EnvironmentToken, environment);


            var password = passwordGenerator.GeneratePassword();
            var id = await accountRepository.Insert(new Account
            {
                Phone = phone,
                Login = phone.ToString(),
                Password = passwordGenerator.HashPassword(password),
                Name = request.Name
            });


            if (request.Roles != null && request.Roles.Any())
            {
                var roles = await roleRepository.GetByRoleNames(request.Roles.ToArray());
                if (roles.Length != request.Roles?.Count)
                    throw new NotFoundException(nameof(request.Roles), string.Join(",",
                        request.Roles.Where(role =>
                            roles.All(r => string.Equals(r.Name, role, StringComparison.OrdinalIgnoreCase)))));

                await roleRepository.AddUserToRoles(id, request.Roles.ToArray());
            }
            else
            {
                await roleRepository.AddUserToDefaultRoles(id);
            }


            await environmentRequestContext.SetUserEnvironment(id, environment);


            var result = mapper.Map<UserProfile>(await accountRepository.FindByLogin(phone.ToString()));
            if (request.BlockSmsSend)
            {
                logger.Info($"User { result?.Id } created. phone: {request.Phone}");
                return result;
            }

            string message;
            if (string.IsNullOrEmpty(request.CustomSmsTemplate))
            {
                message = await messageBuilder.Build(
                    MessageTemplateName.WelcomeTemplate,
                    MessageTemplateType.sms,
                    request.Phone, password);
            }
            else
                message = request.CustomSmsTemplate
                    .Replace("{phone}", request.Phone.ToString())
                    .Replace("{password}", password);

            await smsClient.Send(request.Phone.Value, message, environment?.Token);

            logger.Info($"User { result?.Id } created. New user pin has been sent to phone: {request.Phone}");

            return result;
        }

        public async Task<UserProfile> CreateUser(ForceCreateEmailUserRequest request)
        {
            logger.Info($"User width id: { requestContext.UserId } execute force create user with email: {request.Email}");

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException(nameof(ForceCreateEmailUserRequest.Email));

            var email = request.Email;
            if (await DoesUserExists(email))
            {
                logger.Warn($"Attempt to register an existing user! email: {request.Email}");
                throw new SecurityException("user exists");
            }

            if (!string.IsNullOrWhiteSpace(request.Locale))
            {
                cultureRequestContext.Culture = CultureInfo.GetCultureInfo(request.Locale);
            }

            var environment = await environmentRequestContext.LoadEnvironment(request.EnvironmentToken);
            CheckEnvironmentTokenValid(request.EnvironmentToken, environment);


            var password = passwordGenerator.GeneratePassword();
            var id = await accountRepository.Insert(new Account
            {
                Email = email,
                Login = email,
                Password = passwordGenerator.HashPassword(password),
                Name = request.Name
            });

            if (request.Roles != null && request.Roles.Any())
            {
                var roles = await roleRepository.GetByRoleNames(request.Roles.ToArray());
                if (roles.Length != request.Roles?.Count)
                    throw new NotFoundException(nameof(request.Roles), string.Join(",",
                        request.Roles.Where(role =>
                            roles.All(r => string.Equals(r.Name, role, StringComparison.OrdinalIgnoreCase)))));

                await roleRepository.AddUserToRoles(id, request.Roles.ToArray());
            }
            else
            {
                await roleRepository.AddUserToDefaultRoles(id);
            }

            await environmentRequestContext.SetUserEnvironment(id, environment);

            var result = mapper.Map<UserProfile>(await accountRepository.FindByLogin(email));
            if (request.BlockEmailSend)
            {
                logger.Info($"User { result.Id } created. Email: {request.Email}");
                return result;
            }

            string message;
            string subject;
            if (string.IsNullOrEmpty(request.CustomEmailTemplate)
                || string.IsNullOrEmpty(request.CustomEmailSubjectTemplate))
            {
                subject = await messageBuilder.Build(
                    MessageTemplateName.WelcomeSubjectTemplate,
                    MessageTemplateType.email);

                message = await messageBuilder.Build(
                    MessageTemplateName.WelcomeTemplate,
                    MessageTemplateType.email,
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

            logger.Info($"User { result.Id } created. New user pin has been sent to email: {request.Email}");

            return result;
        }

        public async Task<AccessTokenResponse> GetTokenResponse(Account account)
        {
            var userPermissions = await permissionRepository.GetByUserId(account.Id);
            if (userPermissions == null
                || !userPermissions.Any())
                throw new ForbiddenSecurityException();

            var registration = await tokenRegistrationRepository.FindLastTokenByUserId(account.Id);
            string accessToken = registration?.AccessToken;
            if (!string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    if (registration.ExpiredAt.HasValue
                        && registration.ExpiredAt < DateTime.UtcNow)
                    {
                        accessToken = null;
                    }
                    else
                    {
                        // if permission changed we should make new token
                        var principal = tokenGenerator.Parse(accessToken);
                        if (principal == null
                            || IsRightsDifferent(userPermissions, principal.GetPermissions()))
                            accessToken = null;
                    }
                }
                catch (TokenSecurityException)
                {
                    // if token expired or some sensitive properties changes we should make new token
                    accessToken = null;
                }
            }

            var issueDate = DateTime.UtcNow;
            if (string.IsNullOrEmpty(accessToken))
            {
                var expiredAt = issueDate.Add(tokenGeneratorSettings.Lifetime);
                accessToken = tokenGenerator.GenerateAccessToken(
                    account.Id,
                    issueDate,
                    userPermissions.Select(p => p.Name).ToArray());
                registration = new TokenRegistration
                {
                    UserId = account.Id,
                    IssuedAt = issueDate,
                    AccessToken = accessToken,
                    ExpiredAt = expiredAt
                };
                await tokenRegistrationRepository.Save(registration);
            }
            else
                accessToken = registration.AccessToken;

            var refreshToken = tokenGenerator.GenerateRefreshToken(registration.Id, issueDate, account.Id);
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
            var phone = (decimal)request.Phone;

            // login account
            var account = await accountRepository.FindByLogin(phone.ToString(CultureInfo.InvariantCulture));

            SmsRequest smsRequest;

            if (account != null)
            {
                if (account.IsLocked)
                {
                    var errorText = $"Locked user attempt to login: { request.Phone }";
                    logger.Warn(errorText);
                    throw new UserLockedSecurityException(errorText);
                }

                if (passwordGenerator.CheckPassword(request.Password, account.Password))
                {
                    logger.Info($"User { account.Id } | { request.Phone } logged in");

                    var currentDateTime = DateTime.UtcNow;
                    account.FirstDate = account.FirstDate ?? currentDateTime;
                    account.LastDate = currentDateTime;
                    await accountRepository.Update(account);

                    return await GetTokenResponse(account);
                }
                else
                {
                    // remove try confirm change password
                    smsRequest = await smsRequestRepository.ConfirmRegister(phone, request.Password);
                    if (smsRequest == null)
                    {
                        logger.Warn($"User { account.Id } { request.Phone } wrong password!");
                        return null;
                    }
                }
            }
            else
            {
                // remove try confirm registration
                smsRequest = await smsRequestRepository.ConfirmRegister(phone, request.Password);
                if (smsRequest == null)
                {
                    logger.Warn($"User { request.Phone } wrong registration pin!");
                    return null;
                }
            }


            // confirm sms request
            if (account != null)
            {
                await environmentRequestContext.LoadEnvironmentByUser(account.Id);
                // change password
                var hashPassword = passwordGenerator.HashPassword(request.Password);
                account.Password = hashPassword;
                account.LastDate = DateTime.UtcNow;
                await accountRepository.Update(account);
                logger.Info($"User { account.Id } | { request.Phone } change password successful!");
            }
            else
            {
                var environment = await environmentRequestContext.LoadOrDefault(smsRequest.EnvironmentId.Value);

                // confirm registration
                var password = passwordGenerator.GeneratePassword();
                var hashPassword = passwordGenerator.HashPassword(password);
                var currentDateTime = DateTime.UtcNow;

                var message = await messageBuilder.Build(
                    MessageTemplateName.WelcomeTemplate,
                    MessageTemplateType.sms,
                    request.Phone, password);

                await smsClient.Send(request.Phone, message, environment?.Token);

                var userId = await accountRepository.Insert(account = new Account
                {
                    Phone = phone,
                    Login = phone.ToString(CultureInfo.InvariantCulture),
                    Password = hashPassword,
                    FirstDate = currentDateTime,
                    LastDate = currentDateTime
                });

                // store new assigned Id
                account.Id = userId;

                await roleRepository.AddUserToDefaultRoles(userId);

                await environmentRequestContext.SetUserEnvironment(userId, environment);

                logger.Info($"User { account.Id } | { request.Phone } account created successful!");
            }

            return await GetTokenResponse(account);
        }

        public async Task<AccessTokenResponse> Login(EmailLoginRequest request)
        {
            var email = request.Email;
            var hashPassword = passwordGenerator.HashPassword(request.Password);

            // login account
            var account = await accountRepository.FindByLogin(email);

            EmailRequest emailRequest;

            if (account != null)
            {
                if (account.IsLocked)
                {
                    var errorText = $"Locked user attempt to login: { request.Email }";
                    logger.Warn(errorText);
                    throw new UserLockedSecurityException(errorText);
                }

                if (passwordGenerator.CheckPassword(request.Password, account.Password))
                {
                    logger.Info($"User { account.Id } | { request.Email } logged in");
                    var currentDateTime = DateTime.UtcNow;
                    account.FirstDate = account.FirstDate ?? currentDateTime;
                    account.LastDate = currentDateTime;
                    await accountRepository.Update(account);

                    return await GetTokenResponse(account);
                }
                else
                {
                    // remove try confirm change password
                    emailRequest = await emailRequestRepository.ConfirmRegister(request.Email, request.Password);
                    if (emailRequest == null)
                    {
                        logger.Warn($"User { account.Id } { request.Email } wrong password!");
                        return null;
                    }
                }
            }
            else
            {
                // remove try confirm registration
                emailRequest = await emailRequestRepository.ConfirmRegister(email, request.Password);
                if (emailRequest == null)
                {
                    logger.Warn($"User { request.Email } wrong pin!");
                    return null;
                }
            }


            // confirm email request
            if (account != null)
            {
                await environmentRequestContext.LoadEnvironmentByUser(account.Id);
                // change password
                account.Password = hashPassword;
                account.LastDate = DateTime.UtcNow;
                await accountRepository.Update(account);
                logger.Info($"User { account.Id } | { request.Email } change password successful!");
            }
            else
            {
                var environment = await environmentRequestContext.LoadOrDefault(emailRequest.EnvironmentId);

                var password = passwordGenerator.GeneratePassword();
                hashPassword = passwordGenerator.HashPassword(password);

                var subject = await messageBuilder.Build(
                    MessageTemplateName.WelcomeSubjectTemplate,
                    MessageTemplateType.email);


                var message = await messageBuilder.Build(
                    MessageTemplateName.WelcomeTemplate,
                    MessageTemplateType.email,
                    request.Email, password);

                await emailClient.Send(subject, request.Email, message);

                var currentDateTime = DateTime.UtcNow;

                // confirm registration
                var userId = await accountRepository.Insert(account = new Account
                {
                    Email = email,
                    Login = email,
                    Password = hashPassword,
                    FirstDate = currentDateTime,
                    LastDate = currentDateTime
                });

                // store new assigned Id
                account.Id = userId;

                await roleRepository.AddUserToDefaultRoles(userId);

                await environmentRequestContext.SetUserEnvironment(userId, environment);

                logger.Info($"User { account.Id } | { request.Email } account created successful!");
            }

            return await GetTokenResponse(account);
        }

        public async Task<AccessTokenResponse> Login(ActiveDirectoryUserLoginRequest request, string environmentToken = null)
        {
            var domainUser = await ldapClient.Authorize(request.UserName, request.Password);
            if (domainUser == null)
            {
                logger.Warn($"User {request.UserName} wrong login or password!");
                return null;
            }

            var account = await accountRepository.FindByLogin(domainUser.Email);
            if (account == null)
            {
                var environment = await environmentRequestContext.LoadEnvironment(environmentToken);
                CheckEnvironmentTokenValid(environmentToken, environment);

                var newPassword = passwordGenerator.GeneratePassword();
                var hashPassword = passwordGenerator.HashPassword(newPassword);

               var currentDateTime = DateTime.UtcNow;

                // confirm registration
                var userId = await accountRepository.Insert(account = new Account
                {
                    Email = domainUser.Email,
                    Login = domainUser.Email,
                    Password = hashPassword,
                    FirstDate = currentDateTime,
                    LastDate = currentDateTime
                });

                // store new assigned Id
                account.Id = userId;

                await roleRepository.AddUserToDefaultRoles(userId);

                await environmentRequestContext.SetUserEnvironment(userId, environment);

                logger.Info($"User { account.Id } | { domainUser.Email } account created successful!");
            }
            return await GetTokenResponse(account);
        }

        public async Task ChangePassword(ChangePasswordRequest request)
        {
            var account = await accountRepository.FindByUserId(requestContext.UserId);
            if (account == null)
            {
                logger.Warn($"Attempt to change password for unexisted user { requestContext.UserId }!");
                throw new NotFoundException(nameof(IRequestContext.UserId), "account not found");
            }

            if (account.IsLocked)
            {
                var errorText = $"Attempt to change password for locked user {requestContext.UserId}!";
                logger.Warn(errorText);
                throw new UserLockedSecurityException(errorText);
            }

            if (!passwordGenerator.CheckPassword(request.OldPassword, account.Password))
            {
                logger.Warn($"Attempt to change password for user { requestContext.UserId }. Wrong password!");
                throw new SecurityException("password not match");
            }

            account.Password = passwordGenerator.HashPassword(request.NewPassword);
            await accountRepository.Update(account);
            logger.Warn($"Change password for user { requestContext.UserId } successful!");
        }

        public async Task RequestResetPassword(decimal phone)
        {
            var account = await accountRepository.FindByPhone(phone);
            if (account == null)
            {
                logger.Warn($"Attempt to reset password for unexisted user { phone }!");
                throw new NotFoundException(nameof(phone), "account not found");
            }

            if (account.IsLocked)
            {
                var errorText = $"Attempt to reset password for locked user { account.Id } | { phone }!";
                logger.Warn(errorText);
                throw new UserLockedSecurityException(errorText);
            }

            var existed = await smsRequestRepository.Pick(phone);
            if (existed != null && IsNeedCooldown(existed.ExpiredAt, accountServiceSettings.PinExpirationTime))
            {
                logger.Warn($"User { account.Id } attempt to get password reset pin multiple times! Phone: { phone }");
                return;
            }
            var password = passwordGenerator.GeneratePassword();
            var expirationTime = accountServiceSettings.PinExpirationTime;
            await smsRequestRepository.Save(new SmsRequest
            {
                Phone = phone,
                Password = password,
                ExpiredAt = DateTime.UtcNow.Add(expirationTime)
            });

            await environmentRequestContext.LoadEnvironmentByUser(account.Id);

            var message = await messageBuilder.Build(
                MessageTemplateName.RequestResetTemplate,
                MessageTemplateType.sms,
                password, expirationTime.TotalMinutes);

            var environment = await environmentRequestContext.LoadEnvironment();

            await smsClient.Send(phone, message, environment.Token);
            logger.Info($"User { account.Id } attempt to reset password. New user pin has been sent to phone: { phone }");
        }

        public async Task RequestResetPassword(string email)
        {
            var account = await accountRepository.FindByLogin(email);
            if (account == null)
            {
                logger.Warn($"Attempt to reset password for unexisted user { email }!");
                throw new NotFoundException(nameof(email), "account not found");
            }

            if (account.IsLocked)
            {
                var errorText = $"Attempt to reset password for locked user { account.Id } | { email }!";
                logger.Warn(errorText);
                throw new UserLockedSecurityException(errorText);
            }

            var existed = await emailRequestRepository.Pick(email);
            if (existed != null && IsNeedCooldown(existed.ExpiredAt, accountServiceSettings.EmailCodeExpirationTime))
            {
                logger.Warn($"User { account.Id } attempt to get password reset pin multiple times! Email: { email }");
                return;
            }

            var password = passwordGenerator.GeneratePassword();
            var expirationTime = accountServiceSettings.EmailCodeExpirationTime;
            await emailRequestRepository.Save(new EmailRequest
            {
                Email = email,
                Password = password,
                ExpiredAt = DateTime.UtcNow.Add(expirationTime)
            });

            await environmentRequestContext.LoadEnvironmentByUser(account.Id);

            var subject = await messageBuilder.Build(
                MessageTemplateName.RequestResetSubjectTemplate,
                MessageTemplateType.email,
                "domain0", account.Name);

            var message = await messageBuilder.Build(
                MessageTemplateName.RequestResetTemplate,
                MessageTemplateType.email,
                password, expirationTime.TotalMinutes);

            await emailClient.Send(subject, email, message);
            logger.Info($"User { account.Id } attempt to reset password. New user pin has been sent to email: { email }");
        }

        public async Task RequestChangePhone(ChangePhoneUserRequest changePhoneRequest)
        {
            var accountWithNewPhone = await accountRepository.FindByLogin(changePhoneRequest.Phone.ToString());
            if (accountWithNewPhone != null)
            {
                var warning = $"Attempt to change phone alredy used { changePhoneRequest.Phone }";
                logger.Warn(warning);
                throw new BadModelException(nameof(changePhoneRequest.Phone), warning);
            }

            var userId = requestContext.UserId;
            var account = await accountRepository.FindByUserId(userId);
            if (account == null)
            {
                logger.Warn($"Attempt to change phone for unexisted user { userId }");
                throw new NotFoundException(nameof(account), "account not found");
            }

            if (account.IsLocked)
            {
                var errorText = $"Attempt to change phone for locked user { userId }";
                logger.Warn(errorText);
                throw new UserLockedSecurityException(errorText);
            }

            var existed = await smsRequestRepository.Pick(changePhoneRequest.Phone);
            if (existed != null && IsNeedCooldown(existed.ExpiredAt, accountServiceSettings.PinExpirationTime))
            {
                logger.Warn($"Attempt to change phone multiple times! Phone: {changePhoneRequest.Phone}");
                return;
            }

            if (!passwordGenerator.CheckPassword(changePhoneRequest.Password, account.Password))
            {
                logger.Warn($"User { userId } tries to change phone. But provide a wrong password!");
                throw new SecurityException("password not match");
            }

            var pin = passwordGenerator.GeneratePassword();
            var expirationTime = accountServiceSettings.PinExpirationTime;
            await smsRequestRepository.Save(new SmsRequest
            {
                UserId = userId,
                Phone = changePhoneRequest.Phone,
                Password = pin,
                ExpiredAt = DateTime.UtcNow.Add(expirationTime)
            });

            var environment = await environmentRequestContext.LoadEnvironmentByUser(account.Id);

            var message = await messageBuilder.Build(
                MessageTemplateName.RequestPhoneChangeTemplate,
                MessageTemplateType.sms,
                pin, expirationTime.TotalMinutes);

            await smsClient.Send(changePhoneRequest.Phone, message, environment.Token);
            logger.Info($"Attempt to change phone for user { userId }. New user pin has been sent to phone: { changePhoneRequest.Phone }");
        }

        public async Task CommitChangePhone(long pin)
        {
            var userId = requestContext.UserId;

            var account = await accountRepository.FindByUserId(userId);
            if (account == null)
            {
                logger.Warn($"Attempt to commit change phone for unexisted user {userId}");
                throw new NotFoundException(nameof(account), "account not found");
            }

            if (account.IsLocked)
            {
                var errorText = $"Attempt to commit change phone for locked user { userId }";
                logger.Warn(errorText);
                throw new UserLockedSecurityException(errorText);
            }

            var smsRequest = await smsRequestRepository.PickByUserId(userId);

            if (pin.ToString() != smsRequest?.Password)
            {
                logger.Warn($"Attempt to change phone for user { userId }. Wrong pin!");
                throw new SecurityException("wrong pin");
            }

            account.Phone = smsRequest.Phone;
            account.Login = smsRequest.Phone.ToString(CultureInfo.InvariantCulture);
            await accountRepository.Update(account);
            logger.Warn($"User { userId } changed phone to { account.Phone }");
        }

        public async Task RequestChangeEmail(ChangeEmailUserRequest changeEmailRequest)
        {
            var accountWithNewEmail = await accountRepository.FindByLogin(changeEmailRequest.Email);
            if (accountWithNewEmail != null)
            {
                var warning = $"Attempt to change email alredy used { changeEmailRequest.Email }";
                logger.Warn(warning);
                throw new BadModelException(nameof(changeEmailRequest.Email), warning);
            }

            var userId = requestContext.UserId;
            var account = await accountRepository.FindByUserId(userId);
            if (account == null)
            {
                logger.Warn($"Attempt to change email for unexisted user {userId}");
                throw new NotFoundException(nameof(account), "account not found");
            }

            if (account.IsLocked)
            {
                var errorText = $"Attempt to change email for locked user {userId}";
                logger.Warn(errorText);
                throw new UserLockedSecurityException(errorText);
            }

            if (!passwordGenerator.CheckPassword(changeEmailRequest.Password, account.Password))
            {
                logger.Warn($"User { userId } tries to change email. But provide a wrong password!");
                throw new SecurityException("password not match");
            }

            var existed = await emailRequestRepository.Pick(changeEmailRequest.Email);
            if (existed != null && IsNeedCooldown(existed.ExpiredAt, accountServiceSettings.EmailCodeExpirationTime))
            {
                logger.Warn($"Attempt to change email multiple times! Email: { changeEmailRequest.Email }");
                return;
            }

            var pin = passwordGenerator.GeneratePassword();
            var expirationTime = accountServiceSettings.EmailCodeExpirationTime;
            await emailRequestRepository.Save(new EmailRequest
            {
                UserId = userId,
                Email = changeEmailRequest.Email,
                Password = pin,
                ExpiredAt = DateTime.UtcNow.Add(expirationTime)
            });

            await environmentRequestContext.LoadEnvironmentByUser(account.Id);

            var message = await messageBuilder.Build(
                MessageTemplateName.RequestEmailChangeTemplate,
                MessageTemplateType.email,
                pin, expirationTime.TotalMinutes);

            var subject = await messageBuilder.Build(
                MessageTemplateName.RequestEmailChangeSubjectTemplate,
                MessageTemplateType.email);

            await emailClient.Send(subject, changeEmailRequest.Email, message);
            logger.Info($"Attempt to change phone for user { userId }. New user pin has been sent to phone: { changeEmailRequest.Email }");
        }

        public async Task CommitChangeEmail(long pin)
        {
            var userId = requestContext.UserId;

            var account = await accountRepository.FindByUserId(userId);
            if (account == null)
            {
                logger.Warn($"Attempt to commit change email for unexisted user {userId}");
                throw new NotFoundException(nameof(account), "account not found");
            }

            if (account.IsLocked)
            {
                var errorText = $"Attempt to commit change email for locked user {userId}";
                logger.Warn(errorText);
                throw new UserLockedSecurityException(errorText);
            }

            var emailRequest = await emailRequestRepository.PickByUserId(userId);

            if (pin.ToString() != emailRequest?.Password)
            {
                logger.Warn($"Attempt to change email for user {userId}. Wrong pin!");
                throw new SecurityException("wrong pin");
            }

            account.Login = emailRequest.Email;
            account.Email = emailRequest.Email;
            await accountRepository.Update(account);

            logger.Info($"User { userId } changed email to { account.Email }");
        }


        public async Task ForceChangePhone(ChangePhoneRequest request)
        {
            var accountWithNewPhone = await accountRepository.FindByLogin(request.NewPhone.ToString());
            if (accountWithNewPhone != null)
            {
                var warning = $"Attempt to change phone alredy used { request.NewPhone.ToString() }";
                logger.Warn(warning);
                throw new BadModelException(nameof(request.NewPhone), warning);
            }

            var account = await accountRepository.FindByUserId(request.UserId);
            if (account == null)
            {
                logger.Warn($"User {requestContext.UserId} attempt to force change phone for unexisted user { request.UserId }");
                throw new NotFoundException(nameof(request.UserId), "account not found");
            }

            if (!string.IsNullOrWhiteSpace(request.Locale))
            {
                cultureRequestContext.Culture = CultureInfo.GetCultureInfo(request.Locale);
            }

            if (account.Login == account.Phone.ToString())
                account.Login = request.NewPhone.ToString();
            account.Phone = request.NewPhone;

            await accountRepository.Update(account);
            logger.Info($"User {requestContext.UserId} changed phone for user { request.UserId }");
        }

        public async Task ForceChangeEmail(ChangeEmailRequest request)
        {
            var accountWithNewEmail = await accountRepository.FindByLogin(request.NewEmail);
            if (accountWithNewEmail != null)
            {
                var warning = $"Attempt to change email alredy used { request.NewEmail }";
                logger.Warn(warning);
                throw new BadModelException(nameof(request.NewEmail), warning);
            }

            var account = await accountRepository.FindByUserId(request.UserId);
            if (account == null)
            {
                logger.Warn($"User {requestContext.UserId} attempt to force change email for unexisted user { request.UserId }");
                throw new NotFoundException(nameof(request.UserId), "account not found");
            }

            if (!string.IsNullOrWhiteSpace(request.Locale))
            {
                cultureRequestContext.Culture = CultureInfo.GetCultureInfo(request.Locale);
            }

            if (account.Login == account.Email)
                account.Login = request.NewEmail;
            account.Email = request.NewEmail;

            await accountRepository.Update(account);
            logger.Info($"User {requestContext.UserId} changed email for user { request.UserId }");
        }

        public async Task ForceResetPassword(ForceResetPasswordRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Locale))
            {
                cultureRequestContext.Culture = CultureInfo.GetCultureInfo(request.Locale);
            }

            if (request.UserId.HasValue)
            {
                await ForceResetUserPassword(request.UserId.Value);
            }
            else if (request.Phone.HasValue)
            {
                await ForceResetPassword(request.Phone.Value);
            }
            else if (!string.IsNullOrWhiteSpace(request.Email))
            {
                await ForceResetPassword(request.Email);
            }
            else
            {
                throw new ArgumentException("you must provide one of the following: userId, phone, email");
            }
        }

        public async Task ForceResetUserPassword(int userId)
        {
            var account = await accountRepository.FindByUserId(userId);
            if (account == null)
            {
                logger.Warn($"User { requestContext.UserId } trys reset password for unexisted user { userId }");
                throw new NotFoundException(nameof(userId), "account not found");
            }

            await ForceResetPassword(account, ForceResetPasswordSmsNotify);
        }

        public async Task ForceResetPassword(long phone)
        {
            var account = await accountRepository.FindByPhone(phone);
            if (account == null)
            {
                logger.Warn($"User { requestContext.UserId } trys reset password for unexisted user { phone }");
                throw new NotFoundException(nameof(phone), "account not found");
            }

            await ForceResetPassword(account, ForceResetPasswordSmsNotify);
        }

        public async Task ForceResetPassword(string email)
        {
            var account = await accountRepository.FindByLogin(email);
            if (account == null)
            {
                logger.Warn($"User { requestContext.UserId } tries reset password for unexisted user { email }");
                throw new NotFoundException(nameof(email), "account not found");
            }

            await ForceResetPassword(account, ForceResetPasswordEmailNotify);
        }

        public async Task ForceResetPassword(Account account, Func<Account, string, Task> notify)
        {
            var newPassword = passwordGenerator.GeneratePassword();
            var hashNewPassword = passwordGenerator.HashPassword(newPassword);

            // change password
            account.Password = hashNewPassword;
            await accountRepository.Update(account);
            logger.Info($"User { requestContext.UserId } reset password for user { account.Id }");

            await environmentRequestContext.LoadEnvironmentByUser(account.Id);

            await notify(account, newPassword);
        }

        public async Task ForceResetPasswordSmsNotify(Account account, string newPassword)
        {
            var environment = await environmentRequestContext.LoadEnvironmentByUser(account.Id);

            var message = await messageBuilder.Build(
                MessageTemplateName.ForcePasswordResetTemplate,
                MessageTemplateType.sms,
                newPassword);

            await smsClient.Send(account.Phone.Value, message, environment.Token);
            logger.Info($"New password sent to user { account.Id }");
        }

        public async Task ForceResetPasswordEmailNotify(Account account, string newPassword)
        {
            var message = await messageBuilder.Build(
                MessageTemplateName.ForcePasswordResetTemplate,
                MessageTemplateType.email,
                newPassword);

            var subject = await messageBuilder.Build(
                MessageTemplateName.ForcePasswordResetSubjectTemplate,
                MessageTemplateType.email,
                account.Email, "domain0");

            await emailClient.Send(subject, account.Email, message);
            logger.Info($"New password sent to user { account.Id }");
        }

        public async Task<AccessTokenResponse> Refresh(string prevRefreshToken)
        {
            var id = tokenGenerator.GetTid(prevRefreshToken);
            var tokenRegistry = await tokenRegistrationRepository.FindById(id);
            if (tokenRegistry == null)
            {
                logger.Warn($"User { requestContext.ClientHost } trying to refresh with unexisted or revoked token id: { id }");
                throw new NotFoundException(nameof(tokenRegistry), id);
            }

            var account = await accountRepository.FindByUserId(tokenRegistry.UserId);
            if (account == null)
            {
                logger.Warn($"User { tokenRegistry.UserId } trying to refresh but the user does not exists token id: { id }");
                throw new NotFoundException(nameof(account), tokenRegistry.UserId);
            }

            if (account.IsLocked)
            {
                var errorText = $"User { tokenRegistry.UserId } trying to refresh but the user has been locked";
                logger.Warn(errorText);
                throw new UserLockedSecurityException(errorText);
            }

            var principal = tokenGenerator.Parse(tokenRegistry.AccessToken, skipLifetimeCheck: true);

            var issueDate = DateTime.UtcNow;
            var expiredAt = issueDate.Add(tokenGeneratorSettings.Lifetime);
            var accessToken = tokenGenerator.GenerateAccessToken(
                account.Id,
                issueDate,
                principal.GetPermissions());
            var registration = new TokenRegistration
            {
                UserId = account.Id,
                IssuedAt = issueDate,
                AccessToken = accessToken,
                ExpiredAt = expiredAt
            };
            await tokenRegistrationRepository.Save(registration);
            var refreshToken = tokenGenerator.GenerateRefreshToken(registration.Id, issueDate, account.Id);

            logger.Info($"User { tokenRegistry.UserId } get refreshed token");

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
            {
                logger.Warn($"User { requestContext.UserId } trying to refresh profile of not existed user with phone: { phone }");
                throw new NotFoundException(nameof(account), phone);
            }

            return mapper.Map<UserProfile>(account);
        }

        public async Task<UserProfile> GetProfileByUserId(int id)
        {
            var account = await accountRepository.FindByUserId(id);
            if (account == null)
            {
                logger.Warn($"User { requestContext.UserId } trying to refresh profile of not existed user with id: { id }");
                throw new NotFoundException(nameof(account), id);
            }

            return mapper.Map<UserProfile>(account);
        }

        public async Task<UserProfile[]> GetProfilesByFilter(UserProfileFilter filter)
        {
            var accounts = await accountRepository.FindByUserIds(filter.UserIds);
            return mapper.Map<UserProfile[]>(accounts);
        }

        public async Task<UserProfile> UpdateUser(UserProfile user)
        {
            var oldAccount = await accountRepository.FindByUserId(user.Id);

            if (oldAccount == null)
            {
                logger.Warn($"User { requestContext.UserId } trying to update profile of not existed user with id: { user.Id }");
                throw new NotFoundException(nameof(oldAccount), user.Id);
            }

            var account = oldAccount;

            account.Name = user.Name;
            account.Description = user.Description;

            await accountRepository.Update(account);

            logger.Info($"User { requestContext.UserId } update profile of user: {account.Id}");

            return mapper.Map<UserProfile>(account);
        }

        public async Task DeleteUser(int id)
        {
            logger.Info($"User { requestContext.UserId } removing profile of user: {id}");
            await accountRepository.Delete(id);
            await tokenRegistrationRepository.RevokeByUserId(id);
        }

        public async Task LockUser(int id)
        {
            logger.Info($"User { requestContext.UserId } locking profile of user: {id}");
            await accountRepository.Lock(id);
            await tokenRegistrationRepository.RevokeByUserId(id);
        }

        public async Task UnlockUser(int id)
        {
            logger.Info($"User { requestContext.UserId } unlocking profile of user: {id}");
            await accountRepository.Unlock(id);
        }

        private bool IsNeedCooldown(DateTime expiredAt, TimeSpan expirationTime)
        {
            return (expiredAt - expirationTime - DateTime.UtcNow).Duration() < accountServiceSettings.MessagesResendCooldown;
        }

        private void CheckEnvironmentTokenValid(string environmentToken, Repository.Model.Environment environment)
        {
            if (environment?.Id == null)
            {
                if (string.IsNullOrWhiteSpace(environmentToken))
                {
                    logger.Warn($"Attempt to register user without default token");
                    throw new ArgumentException("unknown default environment token");
                }
                else
                {
                    logger.Warn($"Attempt to register user with unknown environment token: {environmentToken}");
                    throw new ArgumentException("unknown environment token",
                        "EnvironmentToken");
                }
            }
        }

        private readonly IEmailClient emailClient;

        private readonly IEmailRequestRepository emailRequestRepository;

        private readonly IEnvironmentRequestContext environmentRequestContext;

        private readonly ILogger logger;

        private readonly IMapper mapper;

        private readonly IMessageBuilder messageBuilder;

        private readonly ISmsClient smsClient;

        private readonly IPasswordGenerator passwordGenerator;

        private readonly ITokenGenerator tokenGenerator;

        private readonly IRequestContext requestContext;

        private readonly IAccountRepository accountRepository;

        private readonly ICultureRequestContext cultureRequestContext;

        private readonly IRoleRepository roleRepository;

        private readonly ISmsRequestRepository smsRequestRepository;

        private readonly IPermissionRepository permissionRepository;

        private readonly ITokenRegistrationRepository tokenRegistrationRepository;

        private readonly TokenGeneratorSettings tokenGeneratorSettings;

        private readonly AccountServiceSettings accountServiceSettings;

        private readonly ILdapClient ldapClient;

    }
}