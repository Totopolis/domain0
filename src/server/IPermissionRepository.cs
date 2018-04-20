using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface IPermissionRepository
    {
        Task<string[]> GetByUserId(int userId);
    }
}
