using System.ComponentModel.DataAnnotations;
using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Nancy.Model
{
    [Model("Domain user login request")]
    public class DomainUserLoginRequest
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<DomainUserLoginRequest> DefaultDescriptor
            => MessageDescriptor<DomainUserLoginRequest>.Create(new[]
            {
                FieldSetting<DomainUserLoginRequest>.CreateString(1, c => c.UserName, (c, v) => c.UserName = v, c => c.UserName?.Length > 0),
                FieldSetting<DomainUserLoginRequest>.CreateString(2, c => c.Password, (c, v) => c.Password = v, c => c.Password?.Length > 0),
            });

        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }
}