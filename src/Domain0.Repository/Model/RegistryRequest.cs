using System;

namespace Domain0.Repository.Model
{
    public class SmsRequest
    {
        public int? Id { get; set; }

        public decimal Phone { get; set; }

        public string Password { get; set; }

        public DateTime ExpiredAt { get; set; }

        public int? UserId { get; set; }

        public int? EnvironmentId { get; set; }
    }

    public class EmailRequest
    {
        public int? Id { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public DateTime ExpiredAt { get; set; }

        public int? UserId { get; set; }

        public int? EnvironmentId { get; set; }
    }
}
