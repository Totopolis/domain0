using System.Threading.Tasks;
using Domain0.Repository.Model;

namespace Domain0.Service
{
    public interface IMessageBuilder
    {
        Task<string> Build(
            MessageTemplateName templateName, 
            MessageTemplateType templateType, 
            params object[] args);
    }
}
