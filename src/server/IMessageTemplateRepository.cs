using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface IMessageTemplateRepository
    {
        Task<string> GetWelcomeTemplate();

        Task<string> GetRegisterTemplate();

        Task<string> GetRequestResetTemplate();
    }
}
