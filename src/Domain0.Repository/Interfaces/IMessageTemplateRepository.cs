using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Domain0.Repository.Model;

namespace Domain0.Repository
{
    public interface IMessageTemplateRepository
    {
        Task<int> Insert(MessageTemplate entity);
        Task<MessageTemplate[]> FindByIds(IEnumerable<int> ids);
        Task Update(MessageTemplate entity);
        Task Delete(int id);

        Task<string> GetTemplate(
            MessageTemplateName name,
            CultureInfo culture,
            MessageTemplateType type,
            int environmentId);
    }
}
