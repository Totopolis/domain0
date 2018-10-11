using Domain0.Repository.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface IAccountRepository : IRepository<int, Account>
    {
        Task<Account> FindByLogin(string login);

        Task<Account> FindByPhone(decimal phone);

        Task<Account> FindByUserId(int userId);

        new Task<int> Insert(Account account);

        Task<Account[]> FindByUserIds(IEnumerable<int> userIds);
    }
}
