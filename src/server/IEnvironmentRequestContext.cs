using System.Threading.Tasks;
using Domain0.Repository.Model;

namespace Domain0.Service
{
    public interface IEnvironmentRequestContext
    {
        Task<Repository.Model.Environment> LoadEnvironment(string environmentToken);

        Task<Repository.Model.Environment> LoadEnvironment();

        Task SetUserEnvironment(int userId, Environment environment);

        Task SetUserEnvironment(int userId, int environmentId);

        Task SetEnvironment(int value);
    }
}
