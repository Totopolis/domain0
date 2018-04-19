namespace Domain0.Repository.Model
{
    public class Account
    {
        public int Id { get; set; }

        public decimal Phone { get; set; }

        public string Login { get; set; }

        public byte[] Salt { get; set; }

        public string Password { get; set; }

        public string FirstName { get; set; }

        public string SecondName { get; set; }

        public string MiddleName { get; set; }

        public string Description { get; set; }
    }
}
