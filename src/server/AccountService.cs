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
    }

    public class AccountService : IAccountService
    {
        private readonly IMapper _mapper;

        private readonly ISmsClient _smsClient;

        private readonly IPasswordGenerator _passwordGenerator;

        private readonly IAccountRepository _accountRepository;

        private readonly IRoleRepository _roleRepository;

        private readonly IRegistryRequestRepository _registryRequestRepository;

        private readonly IMessageTemplateRepository _messageTemplateRepository;

        public AccountService(
            IMapper mapper,
            ISmsClient smsClient,
            IPasswordGenerator passwordGenerator,
            IAccountRepository accountRepository,
            IRoleRepository roleRepository,
            IRegistryRequestRepository registryRequestRepository,
            IMessageTemplateRepository messageTemplateRepository)
        {
            _mapper = mapper;
            _smsClient = smsClient;
            _passwordGenerator = passwordGenerator;

            _accountRepository = accountRepository;
            _roleRepository = roleRepository;
            _registryRequestRepository = registryRequestRepository;
            _messageTemplateRepository = messageTemplateRepository;
        }

        public async Task Register(decimal phone)
        {
            if (await DoesUserExists(phone))
                throw new SecurityException("user exists");

            var existed = await _registryRequestRepository.Pick(phone);
            if (existed != null)
                return;

            var password = _passwordGenerator.Generate();
            var expiredAt = TimeSpan.FromSeconds(90);
            await _registryRequestRepository.Save(new RegistryRequest
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

            var password = _passwordGenerator.Generate();
            var id = await _accountRepository.Insert(new Account
            {
                Login = request.Phone.ToString(),
                Phone = phone,
                Password = password,
                FirstName = request.Name
            });

            var roles = await _roleRepository.GetByIds(request.Roles.ToArray());
            if (roles.Length != request.Roles.Count)
                throw new NotFoundException(nameof(request.Roles), string.Join(",",
                    request.Roles.Where(role =>
                        roles.All(r => string.Equals(r.Code, role, StringComparison.OrdinalIgnoreCase)))));

            if (roles.Length == 0)
                await _roleRepository.AddUserToRoles(id, "user");
            else
                await _roleRepository.AddUserToRoles(id, request.Roles.ToArray());

            var result = _mapper.Map<UserProfile>(await _accountRepository.FindByPhone(request.Phone.Value));
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
    }
}
