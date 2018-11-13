using Domain0.Repository;
using System.Linq;
using System.Threading.Tasks;
using Domain0.Repository.Model;
using System.Globalization;
using Gerakul.FastSql.Common;
using System;

namespace Domain0.FastSql
{
    public class MessageTemplateRepository : RepositoryBase<int, MessageTemplate>, IMessageTemplateRepository
    {
        public MessageTemplateRepository(Func<DbContext> getContextFunc)
            :base(getContextFunc)
        {
            TableName = "[dom].[Message]";
            KeyName = "Id";
        }

        public async Task<string> GetTemplate(
            MessageTemplateName name,
            CultureInfo culture,
            MessageTemplateType type,
            int environmentId)
        {
            return await GetTemplateInternal(name, culture, type, environmentId);
        }

        private async Task<string> GetTemplateInternal(
            MessageTemplateName name, 
            CultureInfo culture, 
            MessageTemplateType type,
            int environmentId)
        {
            var templates = await getContext()
                .CreateSimple(
                    $"select * " +
                    $"from {TableName} " +
                    $"where {nameof(MessageTemplate.Name)}          =@p0 " +
                    $"  and {nameof(MessageTemplate.Type)}          =@p1 " +
                    $"  and {nameof(MessageTemplate.EnvironmentId)} =@p2 " +
                    $"order by {nameof(MessageTemplate.Locale)} ",
                    name.ToString(),
                    type.ToString(),
                    environmentId)
                .ExecuteQueryAsync<MessageTemplate>()
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