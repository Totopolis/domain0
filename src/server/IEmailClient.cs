using System.Threading.Tasks;

namespace Domain0.Service
{
    public interface IEmailClient
    {
        Task Send(string subject, string emailTo, string message);
    }

    public class EmailClientSettings
    {
        public string ServerHost { get; set; }

        public int Port { get; set; }

        public string Email { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
    }
}
