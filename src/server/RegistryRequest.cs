using System;

namespace Domain0.Repository.Model
{
    public class SmsRequest
    {
        public decimal Phone { get; set; }

        public string Password { get; set; }

        public DateTime ExpiredAt { get; set; }
    }

    public class EmailRequest
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public DateTime ExpiredAt { get; set; }
    }
}
