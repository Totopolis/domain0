using Domain0.Repository;
using Domain0.Repository.Model;
using System;
using System.Security;
using System.Threading.Tasks;

namespace Domain0.Service
{
    public interface IAccountService
    {
        Task Register(decimal phone);

        Task<bool> DoesUserExists(decimal phone);
    }

    public class AccountService : IAccountService
    {
        private readonly ISmsClient _smsClient;

        private readonly IPasswordGenerator _passwordGenerator;

        private readonly IAccountRepository _accountRepository;

        private readonly IRegistryRequestRepository _registryRequestRepository;

        private readonly IMessageTemplateRepository _messageTemplateRepository;

        public AccountService(ISmsClient smsClient,
            IPasswordGenerator passwordGenerator,
            IAccountRepository accountRepository,
            IRegistryRequestRepository registryRequestRepository,
            IMessageTemplateRepository messageTemplateRepository)
        {
            _smsClient = smsClient;
            _passwordGenerator = passwordGenerator;

            _accountRepository = accountRepository;
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
    }
}
