using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Model
{

    [Model("Force change email request")]
    public class ChangeEmailRequest
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<ChangeEmailRequest> DefaultDescriptor
            => MessageDescriptor<ChangeEmailRequest>.Create(new[]
            {
                FieldSetting<ChangeEmailRequest>.CreateInt32(1, c => c.UserId, (c, v) => c.UserId = v),
                FieldSetting<ChangeEmailRequest>.CreateString(2, c => c.NewEmail, (c, v) => c.NewEmail = v),
                FieldSetting<ChangeEmailRequest>.CreateString(
                    3,
                    c => c.Locale,
                    (c, v) => c.Locale = v,
                    c => !string.IsNullOrWhiteSpace(c.Locale)),
            });

        public int UserId { get; set; }

        public string NewEmail { get; set; }

        public string Locale { get; set; }
    }
}
