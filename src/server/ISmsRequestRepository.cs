
using Domain0.Repository.Model;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface ISmsRequestRepository
    {
        Task Save(RegistryRequest registryRequest);

        Task<RegistryRequest> Pick(decimal phone);

        Task<RegistryRequest> Take(decimal phone);
    }
}
