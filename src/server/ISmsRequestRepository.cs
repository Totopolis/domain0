
using Domain0.Repository.Model;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    public interface ISmsRequestRepository
    {
        Task Save(SmsRequest smsRequest);

        Task<SmsRequest> Pick(decimal phone);

        Task Remove(decimal phone);
    }
}
