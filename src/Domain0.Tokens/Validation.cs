using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Domain0.Tokens
{
    public static class Domain0Auth
    {
        public const string Policy = "Domain0";
    }

    public static class ValidationExtensions
    {
        public static TokenValidationParameters BuildTokenValidationParameters(this TokenValidationSettings settings)
        {
            if (settings == null)
                throw new ArgumentException("TokenValidationSettings");

            if (settings.Keys == null)
                throw new ArgumentException("TokenValidationSettings.Keys");

            return new TokenValidationParameters
            {
                SignatureValidator = ValidateSignature,

                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                ValidAudience = settings.Audience,
                ValidIssuer = settings.Issuer,
                ValidateLifetime = settings.ValidateLifetime,
                ClockSkew = settings.ClockSkew,
                IssuerSigningKeys = GetSecurityKeysFromSettings(settings)
            };
        }

        private static SecurityToken ValidateSignature(
            string token,
            TokenValidationParameters validationParameters)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var vp = new TokenValidationParameters
            {
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                ValidAudience = validationParameters.ValidAudience,
                ValidIssuer = validationParameters.ValidIssuer,
                ValidateLifetime = validationParameters.ValidateLifetime,
                ClockSkew = validationParameters.ClockSkew,
                IssuerSigningKeys = validationParameters.IssuerSigningKeys
            };

            var principal = tokenHandler.ValidateToken(token, vp, out SecurityToken securityToken);

            if (!principal.Claims.Any(c => c.Type == "typ" && c.Value == "access_token"))
                throw new UnauthorizedAccessException("typ claim must be 'access_token'");

            return securityToken;
        }

        private static List<SecurityKey> GetSecurityKeysFromSettings(TokenValidationSettings settings)
        {
            var keys = new List<SecurityKey>(settings.Keys.Length);

            foreach (var settingsKey in settings.Keys)
            {
                if (settingsKey.Alg == SecurityAlgorithms.RsaSha256)
                {
                    var publicKeyRsaProvider = new RSACryptoServiceProvider(settingsKey.KeySize ?? 2048);
                    publicKeyRsaProvider.FromXmlString(
                        Encoding.UTF8.GetString(
                            Convert.FromBase64String(settingsKey.Key)));

                    keys.Add(new RsaSecurityKey(publicKeyRsaProvider)
                    {
                        KeyId = settingsKey.Key,
                    });
                }
                else if (settingsKey.Alg == SecurityAlgorithms.HmacSha256)
                {
                    keys.Add(new SymmetricSecurityKey(Convert.FromBase64String(settingsKey.Key))
                    {
                        KeyId = settingsKey.Key,
                    });
                }
            }

            return keys;
        }
    }
}