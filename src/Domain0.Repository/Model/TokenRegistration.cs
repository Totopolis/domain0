
using System;

namespace Domain0.Repository.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class TokenRegistration
    {
        /// <summary>
        /// Идентификатор refresh-токена.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Идентификатор пользователя.
        /// </summary>
        public int UserId { get; set; }
        /// <summary>
        /// Access-токен.
        /// </summary>
        public string AccessToken { get; set; }
        /// <summary>
        /// Дата выдачи refresh-токена.
        /// </summary>
        public DateTime IssuedAt { get; set; }
        /// <summary>
        /// Дата протухания refresh-токена.
        /// </summary>
        public DateTime? ExpiredAt { get; set; }
    }
}
