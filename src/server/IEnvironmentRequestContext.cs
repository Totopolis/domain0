using System.Threading.Tasks;
using Domain0.Repository.Model;

namespace Domain0.Service
{
    public interface IEnvironmentRequestContext
    {
        Task<Environment> LoadEnvironment(int userId);

        Task<Environment> LoadEnvironment(string environmentToken);

        Task<Environment> LoadEnvironment();

        Task SetUserEnvironment(int userId, Environment environment);

        Task SetUserEnvironment(int userId, int environmentId);

        Task SetEnvironment(int environmentId);
    }
}
