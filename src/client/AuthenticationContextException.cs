using System;

namespace Domain0.Api.Client
{
    public class AuthenticationContextException : Exception
    {
        public AuthenticationContextException(string message, Exception innerException) 
            : base(message, innerException) 
        {
        }

        public AuthenticationContextException(string message)
            :base(message)
        {
        }
    }
}
