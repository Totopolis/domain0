﻿using Domain0.Service.Tokens;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

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
        private readonly TokenGeneratorSettings _settings;

        private readonly JwtSecurityTokenHandler _handler;

        private readonly SymmetricSecurityKey _signatureKey;

        public TokenGenerator(TokenGeneratorSettings settings)
        {
            _settings = settings;
            _signatureKey = new SymmetricSecurityKey(Convert.FromBase64String(settings.Secret));
            _handler = new JwtSecurityTokenHandler {SetDefaultTimesOnTokenCreation = false};
        }

        public string GenerateAccessToken(int id, string[] permissions)
            => GenerateAccessToken(id, DateTime.UtcNow, permissions);

        public string GenerateAccessToken(int userId, DateTime issueAt, string[] permissions)
        {
            var claims = new[]
            {
                new Claim(TokenClaims.CLAIM_TOKEN_TYPE, TokenClaims.CLAIM_TOKEN_TYPE_ACCESS),
                new Claim(TokenClaims.CLAIM_SUBJECT, userId.ToString()),
                new Claim(TokenClaims.CLAIM_PERMISSIONS, JsonConvert.SerializeObject(permissions))
            };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                IssuedAt = issueAt,
                Expires = issueAt.Add(_settings.Lifetime),
                Audience = _settings.Audience,
                Issuer = _settings.Issuer,
                Subject = new ClaimsIdentity(claims),
                SigningCredentials = new SigningCredentials(_signatureKey, _settings.Alg)
            };
            var token = _handler.CreateToken(tokenDescriptor);
            return _handler.WriteToken(token);
        }

        public string GenerateRefreshToken(int tokenId, DateTime issueAt, int userId)
        {
            var claims = new[]
            {
                new Claim(TokenClaims.CLAIM_TOKEN_TYPE, TokenClaims.CLAIM_TOKEN_TYPE_REFRESH),
                new Claim(TokenClaims.CLAIM_SUBJECT, userId.ToString()),
                new Claim(TokenClaims.CLAIM_TOKEN_ID, tokenId.ToString()),
            };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                IssuedAt = issueAt,
                Expires = issueAt.Add(_settings.Lifetime),
                Audience = _settings.Audience,
                Issuer = _settings.Issuer,
                Subject = new ClaimsIdentity(claims),
                SigningCredentials = new SigningCredentials(_signatureKey, _settings.Alg)
            };
            var token = _handler.CreateToken(tokenDescriptor);
            return _handler.WriteToken(token);
        }

        public string GenerateRefreshToken(int tokenId, int userId)
            => GenerateRefreshToken(tokenId, DateTime.UtcNow, userId);

        public ClaimsPrincipal Parse(string accessToken)
        {
            var parameters = new TokenValidationParameters
            {
                IssuerSigningKey = _signatureKey,
                NameClaimType = ClaimTypes.Name,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidIssuer = _settings.Issuer,
            };
            var principal = _handler.ValidateToken(accessToken, parameters, out var token);
            var identity = (ClaimsIdentity)principal.Identity;
            identity.AddClaim(new Claim("id_token", accessToken));
            foreach (var role in principal.FindAll(TokenClaims.CLAIM_PERMISSIONS))
            foreach (var permission in JsonConvert.DeserializeObject<string[]>(role.Value))
                identity.AddClaim(new Claim(ClaimTypes.Role, permission));

            var subClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (subClaim != null)
                identity.AddClaim(new Claim(ClaimTypes.Name, subClaim.Value));

            return principal;
        }

        public int GetTid(string refreshToken)
        {
            var parameters = new TokenValidationParameters
            {
                IssuerSigningKey = _signatureKey,
                NameClaimType = ClaimTypes.Name,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidIssuer = _settings.Issuer,
            };
            var principal = _handler.ValidateToken(refreshToken, parameters, out var token);

            // refresh token should have tid field
            var tokenIdClaim = principal.Claims.First(x =>
                x.Type.Equals("http://schemas.microsoft.com/identity/claims/tenantid") 
                || x.Type.Equals(TokenClaims.CLAIM_TOKEN_ID));

            // tid should be a int
            return int.Parse(tokenIdClaim.Value);
        }
    }
}
