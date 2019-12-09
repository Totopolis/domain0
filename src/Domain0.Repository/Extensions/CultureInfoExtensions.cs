using System.Globalization;
using System.Linq;
using Domain0.Repository.Model;

namespace Domain0.Repository.Extensions
{
    public static class CultureInfoExtensions
    {
        public static string GetMatchedTemplate(this CultureInfo culture, MessageTemplate[] templates)
        {
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