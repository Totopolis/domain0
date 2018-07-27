using Domain0.Repository.Model;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface IMessageTemplateRepository
    {
        Task<string> GetWelcomeTemplate(
            MessageTemplateLocale locale,
            MessageTemplateType type);

        Task<string> GetRegisterTemplate(
            MessageTemplateLocale locale,
            MessageTemplateType type);


        Task<string> GetRequestResetTemplate(
            MessageTemplateLocale locale,
            MessageTemplateType type);
    }
}
