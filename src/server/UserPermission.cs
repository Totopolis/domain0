namespace Domain0.Repository.Model
{
    public class UserPermission
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        public int? RoleId { get; set; }

        public int ApplicationId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
