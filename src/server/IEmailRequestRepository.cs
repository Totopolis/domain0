using Domain0.Repository.Model;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface IEmailRequestRepository
    {
        Task Save(EmailRequest emailRequest);

        Task<EmailRequest> Pick(string email);

        Task<bool> ConfirmRegister(string email, string password);

        Task<EmailRequest> PickByUserId(int userId);
    }
}
