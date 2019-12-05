using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;
using System.Collections.Generic;

namespace Domain0.Model
{
    [Model("Message template")]
    public class MessageTemplate
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<MessageTemplate> DefaultDescriptor
            => MessageDescriptor<MessageTemplate>.Create(new[]
            {
                FieldSetting<MessageTemplate>.CreateInt32(1,  c => c.Id.Value,      (c, v) => c.Id = v,         c => c.Id.HasValue),
                FieldSetting<MessageTemplate>.CreateString(2, c => c.Name,          (c, v) => c.Name = v,       c => c.Name?.Length > 0),
                FieldSetting<MessageTemplate>.CreateString(3, c => c.Description,   (c, v) => c.Description = v),
                FieldSetting<MessageTemplate>.CreateString(4, c => c.Type,          (c, v) => c.Type = v),
                FieldSetting<MessageTemplate>.CreateString(5, c => c.Locale,        (c, v) => c.Locale = v),
                FieldSetting<MessageTemplate>.CreateString(6, c => c.Template,      (c, v) => c.Template = v,   c => c.Template?.Length > 0),
                FieldSetting<MessageTemplate>.CreateInt32(7,  c => c.EnvironmentId,      (c, v) => c.EnvironmentId= v)
            });

        /// <summary>
        /// Идентификатор шабона сообщений.
        /// </summary>
        public int? Id { get; set; }
        /// <summary>
        /// Имя шабона.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Описание шабона.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Тип шаблона (sms, email)
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Локализация шаблона
        /// </summary>
        public string Locale { get; set; }
        /// <summary>
        /// Щаблона сообщения
        /// </summary>
        public string Template { get; set; }

        public int EnvironmentId { get; set; }
    }

    [Model("Message template filter")]
    public class MessageTemplateFilter
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<MessageTemplateFilter> DefaultDescriptor
            => MessageDescriptor<MessageTemplateFilter>.Create(new[]
            {
                FieldSetting<MessageTemplateFilter>.CreateInt32Array(
                    1, 
                    c       => c.MessageTemplatesIds, 
                    (c, v)  => c.MessageTemplatesIds.Add(v), 
                    c       => c.MessageTemplatesIds?.Count > 0)
            });

        public MessageTemplateFilter()
        {
        }

        public MessageTemplateFilter(int id)
        {
            MessageTemplatesIds.Add(id);
        }

        public MessageTemplateFilter(IEnumerable<int> ids)
        {
            MessageTemplatesIds.AddRange(ids);
        }

        /// <summary>
        /// Идентификаторы шаблонов.
        /// </summary>
        public List<int> MessageTemplatesIds { get; set; } = new List<int>();
    }
}
