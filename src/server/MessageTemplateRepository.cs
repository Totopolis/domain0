using Domain0.Repository;
using System.Linq;
using System.Threading.Tasks;
using Gerakul.FastSql;
using Domain0.Repository.Model;
using System.Globalization;

namespace Domain0.FastSql
{
    public class MessageTemplateRepository : RepositoryBase<int, MessageTemplate>, IMessageTemplateRepository
    {
        public MessageTemplateRepository(string connectionString) :base(connectionString)
        {
            TableName = "[dom].[Message]";
            KeyName = "Id";
        }

        public async Task<string> GetTemplate(
            MessageTemplateName name,
            CultureInfo culture,
            MessageTemplateType type)
        {
            return await GetTemplateInternal(name, culture, type) 
                // fall back to default culture
                ?? await GetTemplateInternal(name, CultureInfo.CurrentCulture, type);
        }

        private Task<string> GetTemplateInternal(MessageTemplateName name, CultureInfo culture, MessageTemplateType type)
        {
            return SimpleCommand.ExecuteQueryFirstColumnAsync<string>(
                    connectionString,
                    $"select {nameof(MessageTemplate.Template)} " +
                    $"from {TableName} " +
                    $"where {nameof(MessageTemplate.Name)}      =@p0 " +
                    $"  and {nameof(MessageTemplate.Locale)}    =@p1 " +
                    $"  and {nameof(MessageTemplate.Type)}      =@p2 ",
                    name.ToString(),
                    culture.TwoLetterISOLanguageName, 
                    type.ToString())                    
                .FirstOrDefault();
        }
    }
}