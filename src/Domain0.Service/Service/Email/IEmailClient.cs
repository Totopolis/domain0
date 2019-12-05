using System.Threading.Tasks;

namespace Domain0.Service
{
    public interface IEmailClient
    {
        Task Send(string subject, string emailTo, string message);
    }
}
