using Nancy.Validation;

namespace Domain0.Exceptions
{
    public class BadModelException : System.Exception
    {
        public ModelValidationResult ValidationResult { get; }

        public BadModelException(ModelValidationResult validationResult)
        {
            ValidationResult = validationResult;
        }
    }
}
