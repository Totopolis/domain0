using Domain0.Repository.Model;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface IAccountRepository
    {
        Task<Account> FindByPhone(decimal phone);
    }
}
