using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Model
{
    public class UserProfile
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<UserProfile> DefaultDescriptor
            => MessageDescriptor<UserProfile>.Create(new[]
            {
                FieldSetting<UserProfile>.CreateInt32(1, c => c.Id, (c, v) => c.Id = v),
                FieldSetting<UserProfile>.CreateString(2, c => c.Name, (c, v) => c.Name = v, c => c.Name?.Length > 0),
                FieldSetting<UserProfile>.CreateInt64(3, c => (long) c.Phone, (c, v) => c.Phone = v, c => c.Phone != null),
                FieldSetting<UserProfile>.CreateString(4, c => c.Description, (c, v) => c.Description = v, c => c.Description?.Length > 0),
            });

        /// <summary>
        /// Идентификатор пользователя.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Имя пользователя.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Телефон.
        /// </summary>
        public decimal? Phone { get; set; }
        /// <summary>
        /// Описание пользователя.
        /// </summary>
        public string Description { get; set; }
    }
}
