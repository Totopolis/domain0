using Domain0.Repository.Model;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface IMessageTemplateRepository
    {
        Task<string> GetRegisterTemplate();
    }
}
