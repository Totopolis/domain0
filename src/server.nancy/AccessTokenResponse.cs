using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;
using Newtonsoft.Json;

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

        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty(PropertyName = "profile")]
        public UserProfile Profile { get; set; }
    }
}
