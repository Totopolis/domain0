using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Nancy.Model
{
    [Model("Register by email request")]
    public class RegisterRequest
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<RegisterRequest> DefaultDescriptor
            => MessageDescriptor<RegisterRequest>.Create(new[]
            {
                FieldSetting<RegisterRequest>.CreateString(1, c => c.Email, (c, v) => c.Email = v, c => c.Email?.Length > 0)
            });

        public string Email { get; set; }
    }
}
