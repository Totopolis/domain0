using System.Threading.Tasks;

namespace Domain0.Api.Client
{
    public interface IDomain0AuthenticationContext
    {
        IDomain0Client Client { get; }

        string HostUrl { get; set; }

        bool ShouldRemember { get; set; }

        bool IsLoggedIn { get; }

        Task<bool> LoginByPhone(long phone, string password);

        Task<bool> LoginByEmail(string email, string password);

        void Logout();
    }
}