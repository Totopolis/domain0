using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Model
{
    [Model("Force change phone request")]
    public class ChangePhoneRequest
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<ChangePhoneRequest> DefaultDescriptor
            => MessageDescriptor<ChangePhoneRequest>.Create(new[]
            {
                FieldSetting<ChangePhoneRequest>.CreateInt32(1, c => c.UserId, (c, v) => c.UserId = v),
                FieldSetting<ChangePhoneRequest>.CreateInt64(2, c => c.NewPhone, (c, v) => c.NewPhone = v)
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
