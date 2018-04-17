namespace Domain0.Nancy.Model
{
    public class ForceChangePhone
    {
        /// <summary>
        /// Идентификатор пользователя.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Новый телефон.
        /// </summary>
        public long NewPhone { get; set; }
    }
}
