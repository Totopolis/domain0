using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domain0.Model
{
    /// <summary>
    /// Parameters for force email user creation
    /// </summary>
    [Model("Force create email user request")]
    public sealed class ForceCreateEmailUserRequest
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<ForceCreateEmailUserRequest> DefaultDescriptor
            => MessageDescriptor<ForceCreateEmailUserRequest>.Create(new[]
            {
                FieldSetting<ForceCreateEmailUserRequest>.CreateString(1, c => c.Email, (c, v) => c.Email = v, c => !string.IsNullOrWhiteSpace(c.Email)),
                FieldSetting<ForceCreateEmailUserRequest>.CreateString(2, c => c.Name, (c, v) => c.Name = v, c => c.Name?.Length > 0),
                FieldSetting<ForceCreateEmailUserRequest>.CreateBool(3, c => c.BlockEmailSend, (c, v) => c.BlockEmailSend = v),
                FieldSetting<ForceCreateEmailUserRequest>.CreateStringArray(4, c => c.Roles, (c, v) => c.Roles.Add(v), c => c.Roles?.Count > 0),
                FieldSetting<ForceCreateEmailUserRequest>.CreateString(
                    5, 
                    c => c.CustomEmailTemplate, 
                    (c, v) => c.CustomEmailTemplate = v, 
                    c => c.CustomEmailTemplate?.Length > 0),
                FieldSetting<ForceCreateEmailUserRequest>.CreateString(
                    6, 
                    c => c.CustomEmailSubjectTemplate, 
                    (c, v) => c.CustomEmailTemplate = v, 
                    c => c.CustomEmailTemplate?.Length > 0),
                FieldSetting<ForceCreateEmailUserRequest>.CreateString(
                    7,
                    c => c.EnvironmentToken,
                    (c, v) => c.EnvironmentToken= v,
                    c => !string.IsNullOrWhiteSpace(c.EnvironmentToken)),
                FieldSetting<ForceCreateEmailUserRequest>.CreateString(
                    8,
                    c => c.Locale,
                    (c, v) => c.Locale = v,
                    c => !string.IsNullOrWhiteSpace(c.Locale)),
            });

        /// <summary>
        /// User email
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }
        /// <summary>
        /// User name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Prevent sms send to user
        /// </summary>
        public bool BlockEmailSend { get; set; }
        /// <summary>
        /// Roles to assign
        /// </summary>
        public List<string> Roles { get; set; } = new List<string>();
        /// <summary>
        /// Template for email sending
        /// </summary>
        public string CustomEmailTemplate { get; set; }
        /// <summary>
        /// Template for email sending
        /// </summary>
        public string CustomEmailSubjectTemplate { get; set; }
        /// <summary>
        /// User application environment token, determinates scope of user applications and services
        /// </summary>
        public string EnvironmentToken { get; set; }
        /// <summary>
        /// Users locale
        /// </summary>
        public string Locale { get; set; }
    }
}
