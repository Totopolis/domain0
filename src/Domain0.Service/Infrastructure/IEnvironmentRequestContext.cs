using System.Threading.Tasks;
using Domain0.Repository.Model;

namespace Domain0.Service
{
    public interface IEnvironmentRequestContext
    {
        Task<Environment> LoadEnvironmentByUser(int userId);

        Task<Environment> LoadEnvironment(string environmentToken);

        Task<Environment> LoadEnvironment();

        Task<Environment> LoadOrDefault(int? environmentId);

        Task SetUserEnvironment(int userId, Environment environment);

        Task SetUserEnvironment(int userId, int environmentId);

    }
}
