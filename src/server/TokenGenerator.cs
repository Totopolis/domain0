using Domain0.Service.Tokens;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Domain0.Exceptions;
using Domain0.Tokens;

namespace Domain0.Service
{
    public abstract class TokenGenerator : ITokenGenerator, IDisposable
    {
        protected readonly TokenGeneratorSettings Settings;

        private readonly JwtSecurityTokenHandler handler;

        protected TokenGenerator(TokenGeneratorSettings settings)
        {
            Settings = settings;
            handler = new JwtSecurityTokenHandler {SetDefaultTimesOnTokenCreation = false};
        }

        public string GenerateAccessToken(int id, string[] permissions)
            => GenerateAccessToken(id, DateTime.UtcNow, permissions);

        public string GenerateAccessToken(int userId, DateTime issueAt, string[] permissions)
        {
            var claims = BuildClaims(TokenClaims.CLAIM_TOKEN_TYPE_ACCESS, userId, 
                TokenClaims.CLAIM_PERMISSIONS, JsonConvert.SerializeObject(permissions));

            var tokenDescriptor = BuildSecurityTokenDescriptor(Settings.Audience, issueAt, issueAt.Add(Settings.Lifetime), claims);
            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        public string GenerateRefreshToken(int tokenId, DateTime issueAt, int userId)
        {
            var claims = BuildClaims(TokenClaims.CLAIM_TOKEN_TYPE_REFRESH, userId,
                TokenClaims.CLAIM_TOKEN_ID, tokenId.ToString());

            var tokenDescriptor = BuildSecurityTokenDescriptor(Settings.RefreshAudience, issueAt, issueAt.Add(Settings.RefreshLifetime), claims);
            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        public string GenerateRefreshToken(int tokenId, int userId)
            => GenerateRefreshToken(tokenId, DateTime.UtcNow, userId);

        public ClaimsPrincipal Parse(string accessToken, bool skipLifetimeCheck = false)
        {
            try
            {
                var parameters = BuildTokenValidationParameters(Settings.Audience, skipLifetimeCheck);
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
                var parameters = BuildTokenValidationParameters(Settings.RefreshAudience);
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

        protected virtual TokenValidationParameters BuildTokenValidationParameters(
            string audience,
            bool skipLifetimeCheck = false)
        {
            var parameters = new TokenValidationParameters
            {
                NameClaimType = ClaimTypes.Name,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidIssuer = Settings.Issuer,
                ValidateLifetime = !skipLifetimeCheck
            };
            return parameters;
        }

        protected virtual SecurityTokenDescriptor BuildSecurityTokenDescriptor(
            string audience,
            DateTime issueAt, 
            DateTime expireAt, 
            Claim[] claims)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                IssuedAt = issueAt,
                Expires = expireAt,
                Audience = audience,
                Issuer = Settings.Issuer,
                Subject = new ClaimsIdentity(claims),
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

        public virtual void Dispose()
        {
        }
    }

    public class SymmetricKeyTokenGenerator : TokenGenerator
    {
        public SymmetricKeyTokenGenerator(TokenGeneratorSettings settings)
            : base(settings)
        {
            signatureKey = new SymmetricSecurityKey(Convert.FromBase64String(settings.Secret));
        }

        protected override SecurityTokenDescriptor BuildSecurityTokenDescriptor(
            string audience,
            DateTime issueAt, DateTime expireAt, 
            Claim[] claims)
        {
            var securityTokenDescriptor = base.BuildSecurityTokenDescriptor(audience, issueAt, expireAt, claims);
            securityTokenDescriptor.SigningCredentials = new SigningCredentials(signatureKey, SecurityAlgorithms.HmacSha256);
            return securityTokenDescriptor;
        }

        protected override TokenValidationParameters BuildTokenValidationParameters(
            string audience,
            bool skipLifetimeCheck = false)
        {
            var validationProvider = base.BuildTokenValidationParameters(audience, skipLifetimeCheck);
            validationProvider.IssuerSigningKey = signatureKey;
            return validationProvider;
        }

        private readonly SymmetricSecurityKey signatureKey;
    }

    public class AsymmetricKeyPairTokenGenerator : TokenGenerator
    {
        private const int KeySize = 2048;

        public AsymmetricKeyPairTokenGenerator(TokenGeneratorSettings settings)
            : base(settings)
        {
            publicKeyRsaProvider = new RSACryptoServiceProvider(KeySize);
            publicKeyRsaProvider.FromXmlString(
                Encoding.UTF8.GetString(
                    Convert.FromBase64String(settings.Public)));
            publicSecurityKey = new RsaSecurityKey(publicKeyRsaProvider);

            privateKeyRsaProvider = new RSACryptoServiceProvider(KeySize);
            privateKeyRsaProvider.FromXmlString(
                Encoding.UTF8.GetString(
                    Convert.FromBase64String(settings.Private)));
            privateSecurityKey = new RsaSecurityKey(privateKeyRsaProvider);
        }

        protected override SecurityTokenDescriptor BuildSecurityTokenDescriptor(
            string audience,
            DateTime issueAt, DateTime expireAt,
            Claim[] claims)
        {
            var securityTokenDescriptor = base.BuildSecurityTokenDescriptor(audience, issueAt, expireAt, claims);
            securityTokenDescriptor.SigningCredentials = new SigningCredentials(privateSecurityKey, SecurityAlgorithms.RsaSha256);
            return securityTokenDescriptor;
        }

        protected override TokenValidationParameters BuildTokenValidationParameters(
            string audience,
            bool skipLifetimeCheck = false)
        {
            var validationProvider = base.BuildTokenValidationParameters(audience, skipLifetimeCheck);
            validationProvider.IssuerSigningKey = publicSecurityKey;
            return validationProvider;
        }

        public override void Dispose()
        {
            publicKeyRsaProvider?.Dispose();
            privateKeyRsaProvider?.Dispose();
            base.Dispose();
        }

        private readonly RSACryptoServiceProvider publicKeyRsaProvider;
        private readonly RsaSecurityKey publicSecurityKey;
        private readonly RSACryptoServiceProvider privateKeyRsaProvider;
        private readonly RsaSecurityKey privateSecurityKey;
    }
}
