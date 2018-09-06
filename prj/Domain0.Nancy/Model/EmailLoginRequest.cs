using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;
using System.ComponentModel.DataAnnotations;

[Model("email login request")]
public class EmailLoginRequest
{
    [ModelProperty(Ignore = true)]
    public static MessageDescriptor<EmailLoginRequest> DefaultDescriptor
        => MessageDescriptor<EmailLoginRequest>.Create(new[]
        {
                FieldSetting<EmailLoginRequest>.CreateString(1, c => c.Email, (c, v) => c.Email = v, c => c.Email?.Length > 0),
                FieldSetting<EmailLoginRequest>.CreateString(2, c => c.Password, (c, v) => c.Password = v, c => c.Password?.Length > 0),
        });

    [Required]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }
}