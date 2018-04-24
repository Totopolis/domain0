using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace Domain0.Service
{
    public interface ITokenGenerator
    {
        string GenerateAccessToken(int id, string[] permissions);

        string GenerateRefreshToken(int tokenId, int userId);

        ClaimsPrincipal Parse(string accessToken);

        int GetTid(string refreshToken);
    }

    public static class ClaimsPrincipalExtensions
    {
        public static string[] GetPermissions(this ClaimsPrincipal principal)
            => principal.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).Distinct().ToArray();
    }

    public class TokenGeneratorSettings
    {
    }

    public class TokenGenerator : ITokenGenerator
    {
        private readonly TokenGeneratorSettings _settings;

        public TokenGenerator(TokenGeneratorSettings settings)
        {
            _settings = settings;
        }

        public string GenerateAccessToken(int id, string[] permissions)
        {
            throw new NotImplementedException();
        }

        public string GenerateRefreshToken(int tokenId, int userId)
        {
            throw new NotImplementedException();
        }

        public int GetTid(string refreshToken)
        {
            throw new NotImplementedException();
        }

        public ClaimsPrincipal Parse(string accessToken)
        {
            throw new NotImplementedException();
        }
    }
}
