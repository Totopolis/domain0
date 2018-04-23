using Domain0.Repository.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface IAccountRepository
    {
        Task<Account> FindByLogin(string login);

        Task<Account> FindByPhone(decimal phone);

        Task<Account> FindByUserId(int userId);

        Task<int> Insert(Account account);

        Task Update(Account account);

        Task<Account[]> FindByUserIds(IEnumerable<int> userIds);
    }
}
