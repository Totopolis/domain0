using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Model
{
    [Model("Sms login request")]
    public class SmsLoginRequest
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<SmsLoginRequest> DefaultDescriptor
            => MessageDescriptor<SmsLoginRequest>.Create(new[]
            {
                FieldSetting<SmsLoginRequest>.CreateString(1, c => c.Phone, (c, v) => c.Phone = v, c => c.Phone?.Length > 0),
                FieldSetting<SmsLoginRequest>.CreateString(2, c => c.Password, (c, v) => c.Password = v, c => c.Password?.Length > 0),
            });

        public string Phone { get; set; }

        public string Password { get; set; }
    }
}
