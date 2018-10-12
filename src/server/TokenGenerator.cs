using Domain0.Service.Tokens;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Domain0.Exceptions;

namespace Domain0.Service
{
    public class TokenGeneratorSettings
    {
        public string Issuer { get; set; }

        public string Audience { get; set; }

        public TimeSpan Lifetime { get; set; }

        public TimeSpan RefreshLifetime { get; set; }

        public string Secret { get; set; }

        public string Private { get; set; }

        public string Public { get; set; }

        public string Alg { get; set; }
    }

    public class TokenGenerator : ITokenGenerator
    {
        private readonly TokenGeneratorSettings settings;

        private readonly JwtSecurityTokenHandler handler;

        private readonly SymmetricSecurityKey signatureKey;

        public TokenGenerator(TokenGeneratorSettings settings)
        {
            this.settings = settings;
            signatureKey = new SymmetricSecurityKey(Convert.FromBase64String(settings.Secret));
            handler = new JwtSecurityTokenHandler {SetDefaultTimesOnTokenCreation = false};
        }

        public string GenerateAccessToken(int id, string[] permissions)
            => GenerateAccessToken(id, DateTime.UtcNow, permissions);

        public string GenerateAccessToken(int userId, DateTime issueAt, string[] permissions)
        {
            var claims = BuildClaims(TokenClaims.CLAIM_TOKEN_TYPE_ACCESS, userId, 
                TokenClaims.CLAIM_PERMISSIONS, JsonConvert.SerializeObject(permissions));

            var tokenDescriptor = BuildSecurityTokenDescriptor(issueAt, claims);
            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        public string GenerateRefreshToken(int tokenId, DateTime issueAt, int userId)
        {
            var claims = BuildClaims(TokenClaims.CLAIM_TOKEN_TYPE_REFRESH, userId,
                TokenClaims.CLAIM_TOKEN_ID, tokenId.ToString());

            var tokenDescriptor = BuildSecurityTokenDescriptor(issueAt, claims);
            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        public string GenerateRefreshToken(int tokenId, int userId)
            => GenerateRefreshToken(tokenId, DateTime.UtcNow, userId);

        public ClaimsPrincipal Parse(string accessToken)
        {
            try
            {
                var parameters = BuildTokenValidationParameters();
                var principal = handler.ValidateToken(accessToken, parameters, out _);
                var identity = (ClaimsIdentity)principal.Identity;
                identity.AddClaim(new Claim("id_token", accessToken));
                foreach (var role in principal.FindAll(TokenClaims.CLAIM_PERMISSIONS))
                {
                    foreach (var permission in JsonConvert.DeserializeObject<string[]>(role.Value))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, permission));
                    }
                }

                var subClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
                if (subClaim != null)
                    identity.AddClaim(new Claim(ClaimTypes.Name, subClaim.Value));

                return principal;
            }
            catch (Exception ex)
            {
                throw new TokenSecurityException("token parse exception.", ex);
            }
        }

        public int GetTid(string refreshToken)
        {
            try
            {
                var parameters = BuildTokenValidationParameters();
                var principal = handler.ValidateToken(refreshToken, parameters, out _);

                // refresh token should have tid field
                var tokenIdClaim = principal.Claims.First(x =>
                    x.Type.Equals("http://schemas.microsoft.com/identity/claims/tenantid") 
                    || x.Type.Equals(TokenClaims.CLAIM_TOKEN_ID));

                // tid should be a int
                return int.Parse(tokenIdClaim.Value);
            }
            catch (Exception ex)
            {
                throw new TokenSecurityException("token validation exception.", ex);
            }
        }

        private TokenValidationParameters BuildTokenValidationParameters()
        {
            var parameters = new TokenValidationParameters
            {
                IssuerSigningKey = signatureKey,
                NameClaimType = ClaimTypes.Name,
                ValidateAudience = true,
                ValidAudience = settings.Audience,
                ValidIssuer = settings.Issuer,
            };
            return parameters;
        }

        private SecurityTokenDescriptor BuildSecurityTokenDescriptor(DateTime issueAt, Claim[] claims)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                IssuedAt = issueAt,
                Expires = issueAt.Add(settings.Lifetime),
                Audience = settings.Audience,
                Issuer = settings.Issuer,
                Subject = new ClaimsIdentity(claims),
                SigningCredentials = new SigningCredentials(signatureKey, settings.Alg)
            };
            return tokenDescriptor;
        }

        private static Claim[] BuildClaims(string tokenType, int userId, string bodyType, string body)
        {
            var claims = new[]
            {
                new Claim(TokenClaims.CLAIM_TOKEN_TYPE, tokenType),
                new Claim(TokenClaims.CLAIM_SUBJECT, userId.ToString()),
                new Claim(bodyType, body)
            };
            return claims;
        }
    }
}
