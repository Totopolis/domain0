using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Model
{
    /// <summary>
    /// Parameters for force reset user password
    /// </summary>
    [Model("Force reset user request")]
    public sealed class ForceResetPasswordRequest
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<ForceResetPasswordRequest> DefaultDescriptor
            => MessageDescriptor<ForceResetPasswordRequest>.Create(new[]
            {
                FieldSetting<ForceResetPasswordRequest>.CreateInt64(
                    1, 
                    c => c.Phone.Value, 
                    (c, v) => c.Phone = v, 
                    c => c.Phone.HasValue),
                FieldSetting<ForceResetPasswordRequest>.CreateString(
                    2, 
                    c => c.Email, 
                    (c, v) => c.Email = v, 
                    c => !string.IsNullOrWhiteSpace(c.Email)),
                FieldSetting<ForceResetPasswordRequest>.CreateInt32(
                    3,
                    c => c.UserId.Value,
                    (c, v) => c.UserId = v,
                    c => c.UserId.HasValue),
                FieldSetting<ForceResetPasswordRequest>.CreateString(
                    4,
                    c => c.Locale,
                    (c, v) => c.Locale = v,
                    c => !string.IsNullOrWhiteSpace(c.Locale)),
            });

        public long? Phone { get; set; }

        public string Email { get; set; }

        public int? UserId { get; set; }

        public string Locale { get; set; }
    }
}