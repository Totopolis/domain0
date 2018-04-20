
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
    }
}
