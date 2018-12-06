using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Model
{
    [Model("Refresh token request")]
    public class RefreshTokenRequest
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<RefreshTokenRequest> DefaultDescriptor
            => MessageDescriptor<RefreshTokenRequest>.Create(new[]
            {
                FieldSetting<RefreshTokenRequest>.CreateString(1, c => c.RefreshToken, (c, v) => c.RefreshToken = v, c => c.RefreshToken?.Length > 0),
            });

        public string RefreshToken { get; set; }
    }
}
