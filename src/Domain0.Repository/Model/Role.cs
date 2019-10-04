namespace Domain0.Repository.Model
{
    /// <summary>
    /// Роль пользователя.
    /// </summary>
    public class Role
    {
        /// <summary>
        /// Идентификатор роли.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Название роли.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Описание роли.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Роль по умолчанию.
        /// </summary>
        public bool IsDefault { get; set; }
    }
}
