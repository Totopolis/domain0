
namespace Domain0.Repository.Model
{
    public class MessageTemplate
    {
        public int Id { get; set; }

        public string Locale { get; set; }

        public string Type { get; set; }

        public string Template { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
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
