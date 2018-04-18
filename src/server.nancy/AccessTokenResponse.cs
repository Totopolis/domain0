using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Model
{
    [Model("Access token response")]
    public class AccessTokenResponse
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<AccessTokenResponse> DefaultDescriptor
            => MessageDescriptor<AccessTokenResponse>.Create(new[]
            {
                FieldSetting<AccessTokenResponse>.CreateString(1, c => c.AccessToken, (c, v) => c.AccessToken = v, c => c.AccessToken?.Length > 0),
                FieldSetting<AccessTokenResponse>.CreateString(2, c => c.RefreshToken, (c, v) => c.RefreshToken = v, c => c.RefreshToken?.Length > 0),
                FieldSetting<AccessTokenResponse>.CreateMessage(3, c => c.Profile, (c, v) => c.Profile = v, UserProfile.DefaultDescriptor, c => c.Profile != null)
            });

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public UserProfile Profile { get; set; }
    }
}
