using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Model
{
    /// <summary>
    /// Parameters for change user phone
    /// </summary>
    [Model("Request to change phone")]
    public sealed class ChangePhoneUserRequest
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<ChangePhoneUserRequest> DefaultDescriptor
            => MessageDescriptor<ChangePhoneUserRequest>.Create(new[]
            {
                FieldSetting<ChangePhoneUserRequest>.CreateString(
                    1,
                    c => c.Password,
                    (c, v) => c.Password = v,
                    c => !string.IsNullOrWhiteSpace(c.Password)),

                FieldSetting<ChangePhoneUserRequest>.CreateInt64(
                    2,
                    c => c.Phone, (c, v) => c.Phone = v, c => c.Phone > 0),
            });

        public string Password { get; set; }

        public long Phone { get; set; }
    }
}