using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Model
{
    [Model("Change password request")]
    public class ChangePasswordRequest
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<ChangePasswordRequest> DefaultDescriptor
            => MessageDescriptor<ChangePasswordRequest>.Create(new[]
            {
                FieldSetting<ChangePasswordRequest>.CreateString(1, c => c.OldPassword, (c, v) => c.OldPassword = v, c => c.OldPassword?.Length > 0),
                FieldSetting<ChangePasswordRequest>.CreateString(2, c => c.NewPassword, (c, v) => c.NewPassword = v, c => c.NewPassword?.Length > 0),
            });

        public string OldPassword { get; set; }

        public string NewPassword { get; set; }
    }
}
