using Domain0.Repository.Model;
using System.Globalization;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface IMessageTemplateRepository : IRepository<int, MessageTemplate>
    {
        Task<string> GetTemplate(
            MessageTemplateName name,
            CultureInfo culture,
            MessageTemplateType type,
            int environmentId);
    }
}
