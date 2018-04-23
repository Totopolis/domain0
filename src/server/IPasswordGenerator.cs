
using System.Linq;
using System.Security.Claims;

namespace Domain0.Service
{
    public interface IAuthGenerator
    {
        string GeneratePassword();

        bool CheckPassword(string password, string hash, string salt);

        string GenerateAccessToken(int id, string[] permissions);

        string GenerateRefreshToken(int tokenId, int userId);

        string GenerateSalt();

        string HashPassword(string password, string salt);

        ClaimsPrincipal Parse(string accessToken);

        int GetTid(string refreshToken);
    }

    public static class ClaimsPrincipalExtensions
    {
        public static string[] GetPermissions(this ClaimsPrincipal principal)
            => principal.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).Distinct().ToArray();
    }

    public class AuthGenerator : IAuthGenerator
    {
        public bool CheckPassword(string password, string hash, string salt)
        {
            return true;
        }

        public string GenerateAccessToken(int id, string[] permissions)
        {
            return "access_token";
        }

        public string GenerateRefreshToken(int tokenId, int userId)
        {
            return "refresh_token";
        }

        public string GeneratePassword()
        {
            return "password";
        }

        public string GenerateSalt()
        {
            return "salt";
        }

        public string HashPassword(string password, string salt)
        {
            return password + salt;
        }

        public ClaimsPrincipal Parse(string accessToken)
        {
            return new ClaimsPrincipal();
        }

        public int GetTid(string refreshToken)
        {
            return 0;
        }
    }
}
