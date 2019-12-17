using System;
using Microsoft.IdentityModel.Tokens;

namespace Domain0.Tokens
{
    public class TokenValidatorSettings
    {
        public bool ValidateLifetime { get; set; } = true;
        public TimeSpan ClockSkew { get; set; } = TokenValidationParameters.DefaultClockSkew;
    }
}