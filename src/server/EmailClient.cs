using System.Diagnostics;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Domain0.Service
{
    public class EmailClient : IEmailClient
    {
        public EmailClient(EmailClientSettings settingsInstance)
        {
            settings = settingsInstance;
        }

        public async Task Send(string subject, string emailTo, string message)
        {
            Trace.TraceInformation($"email to: { emailTo }, subject: { subject } message: { message }");

            var client = BuildClient();
            var emailMessage = BuildMessage(subject, emailTo, message);

            await client.SendMailAsync(emailMessage);
        }

        private MailMessage BuildMessage(string subject, string emailTo, string message)
        {
            return new MailMessage(
                from: settings.Email,
                to: emailTo,
                subject: subject,
                body: message)
            {
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8,
                DeliveryNotificationOptions = DeliveryNotificationOptions.Never
            };
        }

        private SmtpClient BuildClient()
        {
            return new SmtpClient
            {
                Port = settings.Port,
                Host = settings.ServerHost,
                EnableSsl = true,
                Timeout = 10000,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(
                    settings.Username,
                    settings.Password)
            };
        }

        private readonly EmailClientSettings settings;
    }
}
