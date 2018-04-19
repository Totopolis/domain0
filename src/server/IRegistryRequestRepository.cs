
using Domain0.Repository.Model;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface IRegistryRequestRepository
    {
        Task Save(RegistryRequest registryRequest);

        Task<RegistryRequest> Pick(decimal phone);
    }
}
