namespace Domain0.Repository.Model
{
    public class Permission
    {
        public int Id { get; set; }

        public int ApplicationId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
