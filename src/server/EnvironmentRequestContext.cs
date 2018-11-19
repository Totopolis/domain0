using Domain0.Repository;
using Domain0.Repository.Model;
using Domain0.Service;
using Nancy;
using System.Threading.Tasks;

namespace Domain0.Nancy.Infrastructure
{
    class EnvironmentRequestContext : IEnvironmentRequestContext
    {
        public EnvironmentRequestContext(
            NancyContext nancyContextInstance,
            IEnvironmentRepository environmentRepositoryInstance)
        {
            nancyContext = nancyContextInstance;
            environmentRepository = environmentRepositoryInstance;
        }


        public async Task<Environment> LoadEnvironmentByUser(int userId)
        {
            environment = await environmentRepository.GetByUser(userId);
            return environment;
        }

        public async Task<Environment> LoadEnvironment(string environmentToken)
        {
            environment = null;

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

        public async Task<Environment> LoadEnvironment()
        {
            if (environment != null)
                return environment;

            if (!int.TryParse(nancyContext?.CurrentUser?.Identity?.Name, out var userId))
            {
                environment = await environmentRepository.GetDefault();
            }
            else
            {
                environment = await environmentRepository.GetByUser(userId);
            }

            return environment;
        }

        public async Task<Environment> LoadOrDefault(int? environmentId)
        {
            if (environmentId.HasValue)
            {
                environment = await environmentRepository.FindById(environmentId.Value);
            }
            else
            {
                environment = await environmentRepository.GetDefault();
            }

            return environment;
        }

        public async Task SetUserEnvironment(int userId, Environment newEnvironment)
        {
            environment = newEnvironment;
            if (environment?.Id == null)
                return;
            
            await environmentRepository.SetUserEnvironment(userId, environment.Id.Value);
        }

        public async Task SetUserEnvironment(int userId, int environmentId)
        {
            environment = await environmentRepository.FindById(environmentId);
            if (environment?.Id == null)
                return;

            await environmentRepository.SetUserEnvironment(userId, environment.Id.Value);
        }

        private Environment environment;

        private readonly IEnvironmentRepository environmentRepository;

        private readonly NancyContext nancyContext;
    }
}
