namespace Domain0.Repository.Model
{
    public class UserRole
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsDefault { get; set; }

        public int UserId { get; set; }
    }
}
