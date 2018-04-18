using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Model
{
    public class ForceChangePhone
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<ForceChangePhone> DefaultDescriptor
            => MessageDescriptor<ForceChangePhone>.Create(new[]
            {
                FieldSetting<ForceChangePhone>.CreateInt32(1, c => c.UserId, (c, v) => c.UserId = v),
                FieldSetting<ForceChangePhone>.CreateInt64(2, c => c.NewPhone, (c, v) => c.NewPhone = v),
            });

        /// <summary>
        /// Идентификатор пользователя.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Новый телефон.
        /// </summary>
        public long NewPhone { get; set; }
    }
}
