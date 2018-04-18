using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;
using System.Collections.Generic;

namespace Domain0.Model
{
    /// <summary>
    /// Параметры для принудительного создания пользователя
    /// </summary>
    [Model("Force create user request")]
    public sealed class ForceCreateUserRequest
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<ForceCreateUserRequest> DefaultDescriptor
            => MessageDescriptor<ForceCreateUserRequest>.Create(new[]
            {
                FieldSetting<ForceCreateUserRequest>.CreateInt64(1, c => c.Phone, (c, v) => c.Phone = v),
                FieldSetting<ForceCreateUserRequest>.CreateString(2, c => c.Name, (c, v) => c.Name = v, c => c.Name?.Length > 0),
                FieldSetting<ForceCreateUserRequest>.CreateBool(3, c => c.BlockSmsSend, (c, v) => c.BlockSmsSend = v),
                FieldSetting<ForceCreateUserRequest>.CreateStringArray(4, c => c.Roles, (c, v) => c.Roles.Add(v), c => c.Roles?.Count > 0),
                FieldSetting<ForceCreateUserRequest>.CreateString(5, c => c.CustomSmsTemplate, (c, v) => c.CustomSmsTemplate = v, c => c.CustomSmsTemplate?.Length > 0),
            });

        /// <summary>
        /// Телефон пользователя
        /// </summary>
        public long Phone { get; set; }
        /// <summary>
        /// Имя пользователя
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Заблокировать отправку сообщения пользователю
        /// </summary>
        public bool BlockSmsSend { get; set; }
        /// <summary>
        /// Роли, которые будут назначены пользователю
        /// </summary>
        public List<string> Roles { get; set; }
        /// <summary>
        /// Шаблон сообщения который будет отправлен пользователю
        /// </summary>
        public string CustomSmsTemplate { get; set; }
    }
}