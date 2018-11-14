namespace Domain0.Repository.Model
{
    public class Environment
    {
        public int? Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Token { get; set; }

        public bool IsDefault { get; set; }
    }
}
