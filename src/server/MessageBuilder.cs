using System.Threading.Tasks;
using Domain0.Repository;
using Domain0.Repository.Model;

namespace Domain0.Service
{
    public class MessageBuilder : IMessageBuilder
    {
        public MessageBuilder(
            ICultureRequestContext cultureRequestContext,
            IEnvironmentRequestContext environmentContext,
            IMessageTemplateRepository messageTemplateRepository)
        {
            cultureContext = cultureRequestContext;
            environment = environmentContext;
            templateRepository = messageTemplateRepository;
        }

        public async Task<string> Build(
            MessageTemplateName templateName, 
            MessageTemplateType templateType, 
            params object[] args)
        {
            var messageTemplate = await templateRepository.GetTemplate(
                templateName,
                cultureContext.Culture, 
                templateType);
            
            var message = string.Format(messageTemplate, args);

            return message;
        }

        private readonly ICultureRequestContext cultureContext;
        private readonly IEnvironmentRequestContext environment;
        private readonly IMessageTemplateRepository templateRepository;
    }
}
