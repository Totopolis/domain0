using System;
using System.Security.Claims;

namespace Domain0.Service
{
    public interface ITokenGenerator
    {
        string GenerateAccessToken(int id, DateTime issueAt, string[] permissions);

        string GenerateAccessToken(int id, string[] permissions);

        string GenerateRefreshToken(int tokenId, int userId);

        string GenerateRefreshToken(int tokenId, DateTime issueAt, int userId);

        ClaimsPrincipal Parse(string accessToken);

        int GetTid(string refreshToken);
    }
}