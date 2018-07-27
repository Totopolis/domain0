
namespace Domain0.Repository.Model
{
    public class MessageTemplate
    {
        public string Locale { get; set; }

        public string Type { get; set; }

        public string Template { get; set; }

        public string Name { get; set; }
    }

    public enum MessageTemplateLocale
    {
        rus, eng
    };

    public enum MessageTemplateType
    {
        sms
    };

}
