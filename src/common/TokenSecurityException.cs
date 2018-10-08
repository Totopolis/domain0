using System;

namespace Domain0.Exceptions
{
    public class TokenParseException : Exception
    {
        public TokenParseException(string message, Exception ex)
            : base(message, ex)
        {

        }
    }
}
