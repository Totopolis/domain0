using Nancy;
using Nancy.Validation;

namespace Domain0.Exceptions
{
    public class BadModelException : System.Exception
    {
        public ModelValidationResult ValidationResult { get; }

        public BadModelException(string field, string message)
        {
            ValidationResult = new ModelValidationResult();
            ValidationResult.Errors.Add(field, message);
        }

        public BadModelException(ModelValidationResult validationResult)
        {
            ValidationResult = validationResult;
        }
    }
}
