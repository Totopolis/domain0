using System;

namespace Domain0.Api.Client
{
    public class Domain0AuthenticationContextException : Exception
    {
        public Domain0AuthenticationContextException(string message, Exception innerException) 
            : base(message, innerException) 
        {
        }

        public Domain0AuthenticationContextException(string message)
            :base(message)
        {
        }
    }
}
