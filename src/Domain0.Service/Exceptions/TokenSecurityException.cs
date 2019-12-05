using System;

namespace Domain0.Exceptions
{
    public class TokenSecurityException : Exception
    {
        public TokenSecurityException(string message, Exception ex)
            : base(message, ex)
        {

        }
    }
}
