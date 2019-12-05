using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;
using System.Collections.Generic;

namespace Domain0.Model
{
    [Model("Application")]
    public class Application
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<Application> DefaultDescriptor
            => MessageDescriptor<Application>.Create(new[]
            {
                FieldSetting<Application>.CreateInt32(1,
                    c => c.Id.Value,
                    (c, v) => c.Id = v,
                    c => c.Id.HasValue),
                FieldSetting<Application>.CreateString(2,
                    c => c.Name,
                    (c, v) => c.Name = v,
                    c => c.Name?.Length > 0),
                FieldSetting<Application>.CreateString(3,
                    c => c.Description,
                    (c, v) => c.Description = v),
            });

        /// <summary>
        /// Идентификатор
        /// </summary>
        public int? Id { get; set; }
        /// <summary>
        /// Имя
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Описание
        /// </summary>
        public string Description { get; set; }
    }

    [Model("Application filter")]
    public class ApplicationFilter
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<ApplicationFilter> DefaultDescriptor
            => MessageDescriptor<ApplicationFilter>.Create(new[]
            {
                FieldSetting<ApplicationFilter>.CreateInt32Array(
                    1,
                    c       => c.ApplicationIds,
                    (c, v)  => c.ApplicationIds.Add(v),
                    c       => c.ApplicationIds?.Count > 0)
            });

        public ApplicationFilter()
        {
        }

        public ApplicationFilter(int id)
        {
            ApplicationIds.Add(id);
        }

        public ApplicationFilter(IEnumerable<int> ids)
        {
            ApplicationIds.AddRange(ids);
        }

        /// <summary>
        /// Идентификаторы шаблонов.
        /// </summary>
        public List<int> ApplicationIds { get; set; } = new List<int>();
    }
}
