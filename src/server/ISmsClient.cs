using System.Threading.Tasks;

namespace Domain0.Service
{
    public interface ISmsClient
    {
        Task Send(decimal phone, string message, string environment);
    }
}
