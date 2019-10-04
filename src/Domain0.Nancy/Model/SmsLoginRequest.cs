using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Domain0.Model
{
    [Model("Sms login request")]
    public class SmsLoginRequest
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<SmsLoginRequest> DefaultDescriptor
            => MessageDescriptor<SmsLoginRequest>.Create(new[]
            {
                FieldSetting<SmsLoginRequest>.CreateInt64(1, c => c.Phone, (c, v) => c.Phone = v),
                FieldSetting<SmsLoginRequest>.CreateString(2, c => c.Password, (c, v) => c.Password = v, c => c.Password?.Length > 0),
            });

        [Required]
        public long Phone { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
