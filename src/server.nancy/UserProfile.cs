namespace Domain0.Model
{
    public class UserProfile
    {
        /// <summary>
        /// Идентификатор пользователя.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Имя пользователя.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Телефон.
        /// </summary>
        public decimal? Phone { get; set; }
        /// <summary>
        /// Описание пользователя.
        /// </summary>
        public string Description { get; set; }
    }
}
