namespace Domain0.Repository.Model
{
    public class Account
    {
        public int Id { get; set; }

        public decimal Phone { get; set; }

        public string Login { get; set; }

        public string Salt { get; set; }

        public string Password { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
