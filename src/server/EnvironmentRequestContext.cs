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


        public async Task<Repository.Model.Environment> LoadEnvironment(string environmentToken)
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

        public async Task<Repository.Model.Environment> LoadEnvironment()
        {
            if (environment != null)
                return environment;

            if (int.TryParse(nancyContext.CurrentUser.Identity.Name, out int userId))
            {
                var environment = await environmentRepository.GetByUser(userId);
            }

            return null;
        }

        public async Task SetUserEnvironment(int userId, Environment newEnvironment)
        {
            environment = newEnvironment;
            await environmentRepository.SetUserEnvironment(userId, environment.Id.Value);
        }

        public async Task SetUserEnvironment(int userId, int environmentId)
        {
            environment = await environmentRepository.FindById(environmentId);
            await environmentRepository.SetUserEnvironment(userId, environment.Id.Value);
        }

        public async Task SetEnvironment(int environmentId)
        {
            environment = await environmentRepository.FindById(environmentId);
        }

        private Repository.Model.Environment environment;

        private readonly IEnvironmentRepository environmentRepository;

        private readonly NancyContext nancyContext;
    }
}
