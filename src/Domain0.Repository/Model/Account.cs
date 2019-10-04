using System;

namespace Domain0.Repository.Model
{
    public class Account
    {
        public int Id { get; set; }

        public string Email { get; set; }

        public decimal? Phone { get; set; }

        public string Login { get; set; }

        public string Password { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime? FirstDate { get; set; }

        public DateTime? LastDate { get; set; }

        public bool IsLocked { get; set; }
    }
}
