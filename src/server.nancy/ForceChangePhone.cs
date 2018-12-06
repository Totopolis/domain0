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
                FieldSetting<ChangePhoneRequest>.CreateInt64(2, c => c.NewPhone, (c, v) => c.NewPhone = v),
                FieldSetting<ChangePhoneRequest>.CreateString(
                    3,
                    c => c.Locale,
                    (c, v) => c.Locale = v,
                    c => !string.IsNullOrWhiteSpace(c.Locale)),
            });

        public int UserId { get; set; }

        public long NewPhone { get; set; }

        public string Locale { get; set; }
    }
}
