using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Model
{
    /// <summary>
    /// Parameters for change email
    /// </summary>
    [Model("Request to change email")]
    public sealed class ChangeEmailUserRequest
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<ChangeEmailUserRequest> DefaultDescriptor
            => MessageDescriptor<ChangeEmailUserRequest>.Create(new[]
            {
                FieldSetting<ChangeEmailUserRequest>.CreateString(
                    1,
                    c => c.Password,
                    (c, v) => c.Password = v,
                    c => !string.IsNullOrWhiteSpace(c.Password)),

                FieldSetting<ChangeEmailUserRequest>.CreateString(
                    2,
                    c => c.Email, (c, v) => c.Email = v, c => !string.IsNullOrWhiteSpace(c.Email)),
            });

        public string Password { get; set; }

        public string Email { get; set; }
    }
}
