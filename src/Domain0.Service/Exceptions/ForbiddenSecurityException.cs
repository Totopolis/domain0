using System;

namespace Domain0.Exceptions
{
    public class ForbiddenSecurityException : Exception
    {
        public ForbiddenSecurityException()
        {
        }
        public ForbiddenSecurityException(string message)
            : base(message)
        {
        }
    }
}
