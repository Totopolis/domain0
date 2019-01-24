using System.ComponentModel.DataAnnotations;
using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Nancy.Model
{
    [Model("Domain user login request")]
    public class ActiveDirectoryUserLoginRequest
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<ActiveDirectoryUserLoginRequest> DefaultDescriptor
            => MessageDescriptor<ActiveDirectoryUserLoginRequest>.Create(new[]
            {
                FieldSetting<ActiveDirectoryUserLoginRequest>.CreateString(1, c => c.UserName, (c, v) => c.UserName = v, c => c.UserName?.Length > 0),
                FieldSetting<ActiveDirectoryUserLoginRequest>.CreateString(2, c => c.Password, (c, v) => c.Password = v, c => c.Password?.Length > 0),
            });

        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }
}