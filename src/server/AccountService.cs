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
using NLog;

namespace Domain0.Service
{
    public static class ClaimsPrincipalExtensions
    {
        public static string[] GetPermissions(this ClaimsPrincipal principal)
            => principal.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).Distinct().ToArray();
    }

    public class AccountServiceSettings
    {
        public TimeSpan MessagesResendCooldown { get; internal set; }

        public TimeSpan PinExpirationTime { get; set; }

        public TimeSpan EmailCodeExpirationTime { get; set; }
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
            IEnvironmentRepository environmentRepositoryInstance,
            ILogger loggerInstance,
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
            environmentRepository = environmentRepositoryInstance;
            logger = loggerInstance;
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

            var environment = await GetEnvironment(environmentToken);

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

            var template = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RegisterTemplate,
                cultureRequestContext.Culture, 
                MessageTemplateType.sms);
            var message = string.Format(template, password, expirationTime.TotalMinutes);

            await smsClient.Send(phone, message);
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

            var environment = await GetEnvironment(environmentToken);

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

            var subjectTemplate = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RegisterSubjectTemplate,
                cultureRequestContext.Culture,
                MessageTemplateType.email);
            var template = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RegisterTemplate,
                cultureRequestContext.Culture,
                MessageTemplateType.email);

            var message = string.Format(template, password, expirationTime.TotalMinutes);
            var subject = string.Format(subjectTemplate, email, "domain0");

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
                throw new ArgumentException(nameof(ForceCreateUserRequest.Phone));

            var phone = request.Phone.Value;
            if (await DoesUserExists(phone))
            {
                logger.Warn($"Attempt to register an existing user! phone: {request.Phone}");
                throw new SecurityException("user exists");
            }

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

            var result = mapper.Map<UserProfile>(await accountRepository.FindByLogin(phone.ToString()));
            if (request.BlockSmsSend)
            {
                logger.Info($"User { result?.Id } created. phone: {request.Phone}");
                return result;
            }

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
            var phone = (decimal)request.Phone;

            // login account
            var account = await accountRepository.FindByLogin(phone.ToString());
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
                    if (!await smsRequestRepository.ConfirmRegister(phone, request.Password))
                    {
                        logger.Warn($"User { account.Id } { request.Phone } wrong password!");
                        return null;
                    }
                }
            }
            else
            {
                // remove try confirm registration
                if (!await smsRequestRepository.ConfirmRegister(phone, request.Password))
                {
                    logger.Warn($"User { request.Phone } wrong registration pin!");
                    return null;
                }
            }


            // confirm sms request
            if (account != null)
            {
                // change password
                var hashPassword = passwordGenerator.HashPassword(request.Password);
                account.Password = hashPassword;
                account.LastDate = DateTime.UtcNow;
                await accountRepository.Update(account);
                logger.Info($"User { account.Id } | { request.Phone } change password successful!");
            }
            else
            {
                // confirm registration
                var password = passwordGenerator.GeneratePassword();
                var hashPassword = passwordGenerator.HashPassword(password);
                var currentDateTime = DateTime.UtcNow;

                var message = string.Format(await messageTemplateRepository.GetTemplate(
                    MessageTemplateName.WelcomeTemplate,
                    cultureRequestContext.Culture,
                    MessageTemplateType.sms),
                    request.Phone, password);

                await smsClient.Send(request.Phone, message);

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
                    if (!await emailRequestRepository.ConfirmRegister(request.Email, request.Password))
                    {
                        logger.Warn($"User { account.Id } { request.Email } wrong password!");
                        return null;
                    }
                }
            }
            else
            {
                // remove try confirm registration
                if (!await emailRequestRepository.ConfirmRegister(email, request.Password))
                {
                    logger.Warn($"User { request.Email } wrong pin!");
                    return null;
                }
            }

            // confirm email request
            if (account != null)
            {
                // change password
                account.Password = hashPassword;
                account.LastDate = DateTime.UtcNow;
                await accountRepository.Update(account);
                logger.Info($"User { account.Id } | { request.Email } change password successful!");
            }
            else
            {
                var password = passwordGenerator.GeneratePassword();
                hashPassword = passwordGenerator.HashPassword(password);

                var subject = await messageTemplateRepository.GetTemplate(
                    MessageTemplateName.WelcomeSubjectTemplate,
                    cultureRequestContext.Culture,
                    MessageTemplateType.email);

                var message = string.Format(await messageTemplateRepository.GetTemplate(
                        MessageTemplateName.WelcomeTemplate,
                        cultureRequestContext.Culture,
                        MessageTemplateType.email),
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
                logger.Info($"User { account.Id } | { request.Email } account created successful!");
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
                logger.Warn($"User { account.Id } attempt to get pasword reset pin multiple times! Phone: { phone }");
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

            var template = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RequestResetTemplate,
                cultureRequestContext.Culture,
                MessageTemplateType.sms);

            var message = string.Format(template, password, expirationTime.TotalMinutes);
            await smsClient.Send(phone, message);
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
                logger.Warn($"User { account.Id } attempt to get pasword reset pin multiple times! Email: { email }");
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

            var subjectTemplate = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RequestResetSubjectTemplate,
                cultureRequestContext.Culture, 
                MessageTemplateType.email);

            var template = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RequestResetTemplate,
                cultureRequestContext.Culture, 
                MessageTemplateType.email);

            var message = string.Format(template, password, expirationTime.TotalMinutes);
            var subject = string.Format(subjectTemplate, "domain0", account.Name);

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

            var template = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RequestPhoneChangeTemplate,
                cultureRequestContext.Culture,
                MessageTemplateType.sms);

            var message = string.Format(template, pin, expirationTime.TotalMinutes);
            await smsClient.Send(changePhoneRequest.Phone, message);
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

            var template = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RequestEmailChangeTemplate,
                cultureRequestContext.Culture,
                MessageTemplateType.email);

            var subject = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.RequestEmailChangeSubjectTemplate,
                cultureRequestContext.Culture,
                MessageTemplateType.email);

            var message = string.Format(template, pin, expirationTime.TotalMinutes);
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

            if (account.Login == account.Email)
                account.Login = request.NewEmail;
            account.Email = request.NewEmail;

            await accountRepository.Update(account);
            logger.Info($"User {requestContext.UserId} changed email for user { request.UserId }");
        }

        public async Task ForceResetPassword(long phone)
        {
            var account = await accountRepository.FindByPhone(phone);
            if (account == null)
            {
                logger.Warn($"User { requestContext.UserId } trys reset password for unexisted user { phone }");
                throw new NotFoundException(nameof(phone), "account not found");
            }

            var newPassword = passwordGenerator.GeneratePassword();
            var hashNewPassword = passwordGenerator.HashPassword(newPassword);

            // change password
            account.Password = hashNewPassword;
            await accountRepository.Update(account);
            logger.Info($"User { requestContext.UserId } reset password for user { account.Id }");

            var template = await messageTemplateRepository.GetTemplate(
                MessageTemplateName.ForcePasswordResetTemplate,
                cultureRequestContext.Culture,
                MessageTemplateType.sms);

            var message = string.Format(template, newPassword);
            await smsClient.Send(phone, message);
            logger.Info($"New password sent to user { account.Id }");
        }

        public async Task ForceResetPassword(string email)
        {
            var account = await accountRepository.FindByLogin(email);
            if (account == null)
            {
                logger.Warn($"User { requestContext.UserId } tries reset password for unexisted user { email }");
                throw new NotFoundException(nameof(email), "account not found");
            }

            var newPassword = passwordGenerator.GeneratePassword();
            var hashNewPassword = passwordGenerator.HashPassword(newPassword);

            // change password
            account.Password = hashNewPassword;
            await accountRepository.Update(account);
            logger.Info($"User { requestContext.UserId } reset password for user { account.Id }");


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
            logger.Info($"New password sent to user { account.Id }");
        }

        public async Task<AccessTokenResponse> Refresh(string refreshToken)
        {
            var id = tokenGenerator.GetTid(refreshToken);
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
            var accessToken = tokenGenerator.GenerateAccessToken(account.Id, principal.GetPermissions());

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

        private async Task<Repository.Model.Environment> GetEnvironment(string environmentToken)
        {
            Repository.Model.Environment environment = null;
            if (!string.IsNullOrWhiteSpace(environmentToken))
            {
                environment = await environmentRepository.GetByToken(environmentToken);
            }
            else
            {
                environment = await environmentRepository.GetDefault();
            }

            return environment;
        }

        private readonly IEmailClient emailClient;

        private readonly IEmailRequestRepository emailRequestRepository;

        private readonly IEnvironmentRepository environmentRepository;

        private readonly ILogger logger;

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