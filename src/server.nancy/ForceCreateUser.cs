using System.Collections.Generic;

namespace Domain0.Model
{
    /// <summary>
    /// Параметры для принудительного создания пользователя
    /// </summary>
    public sealed class ForceCreateUserRequest
    {
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