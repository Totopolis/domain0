using System;

namespace Domain0.Exceptions
{
    public class UserLockedSecurityException : Exception
    {
        public UserLockedSecurityException(string message)
            : base(message)
        {

        }
    }
}
