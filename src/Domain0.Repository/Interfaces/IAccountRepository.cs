using Domain0.Repository.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface IAccountRepository
    {
        Task<int> Insert(Account account);
        Task<Account> FindByLogin(string login);
        Task<Account> FindByPhone(decimal phone);
        Task<Account> FindByUserId(int userId);
        Task<Account[]> FindByUserIds(IEnumerable<int> userIds);
        Task Update(Account entity);
        Task Delete(int id);

        Task Lock(int userId);

        Task Unlock(int userId);
    }
}
