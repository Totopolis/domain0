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
            return await GetTemplateInternal(name, culture, type);
        }

        private async Task<string> GetTemplateInternal(MessageTemplateName name, CultureInfo culture, MessageTemplateType type)
        {
            var templates = await SimpleCommand.ExecuteQueryAsync<MessageTemplate>(
                    connectionString,
                    $"select {nameof(MessageTemplate.Template)} " +
                    $"from {TableName} " +
                    $"where {nameof(MessageTemplate.Name)}      =@p0 " +
                    $"  and {nameof(MessageTemplate.Type)}      =@p2 ",
                    name.ToString(),
                    type.ToString())                    
                .ToArray();

            return GetTemplateMatch(culture, templates) ??
                   // fall back to default culture
                   GetTemplateMatch(CultureInfo.CurrentCulture, templates);
        }

        private static string GetTemplateMatch(CultureInfo culture, MessageTemplate[] templates)
        {
            var fullyCompatibleLocale = templates.FirstOrDefault(t => culture.Name == t.Locale);
            if (fullyCompatibleLocale != null)
                return fullyCompatibleLocale.Template;

            var generallyCompatibleLocale = templates.FirstOrDefault(t => culture.TwoLetterISOLanguageName == t.Locale);
            if (generallyCompatibleLocale != null)
                return generallyCompatibleLocale.Template;

            var partlyCompatibleLocale = templates.FirstOrDefault(t => t.Locale.StartsWith(culture.TwoLetterISOLanguageName));
            if (partlyCompatibleLocale != null)
                return partlyCompatibleLocale.Template;

            return null;
        }
    }
}