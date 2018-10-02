using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domain0.Model
{
    /// <summary>
    /// Parameters for force user creation
    /// </summary>
    [Model("Force create user request")]
    public sealed class ForceCreateUserRequest
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<ForceCreateUserRequest> DefaultDescriptor
            => MessageDescriptor<ForceCreateUserRequest>.Create(new[]
            {
                FieldSetting<ForceCreateUserRequest>.CreateInt64(1, c => c.Phone.Value, (c, v) => c.Phone = v, c => c.Phone.HasValue),
                FieldSetting<ForceCreateUserRequest>.CreateString(2, c => c.Name, (c, v) => c.Name = v, c => c.Name?.Length > 0),
                FieldSetting<ForceCreateUserRequest>.CreateBool(3, c => c.BlockSmsSend, (c, v) => c.BlockSmsSend = v),
                FieldSetting<ForceCreateUserRequest>.CreateStringArray(4, c => c.Roles, (c, v) => c.Roles.Add(v), c => c.Roles?.Count > 0),
                FieldSetting<ForceCreateUserRequest>.CreateString(5, c => c.CustomSmsTemplate, (c, v) => c.CustomSmsTemplate = v, c => c.CustomSmsTemplate?.Length > 0),
            });

        /// <summary>
        /// User phone number
        /// </summary>
        [Required(ErrorMessage = "Phone is required")]
        [MinLength(11, ErrorMessage = "Phone number at least 11 digits without +")]
        public long? Phone { get; set; }
        /// <summary>
        /// User name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Prevent sms send to user
        /// </summary>
        public bool BlockSmsSend { get; set; }
        /// <summary>
        /// Roles to assign
        /// </summary>
        public List<string> Roles { get; set; } = new List<string>();
        /// <summary>
        /// Template for sms sending
        /// </summary>
        public string CustomSmsTemplate { get; set; }
    }
}