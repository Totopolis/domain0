using Nancy;
using Domain0.Exceptions;
using Nancy.ModelBinding;

namespace Domain0.Nancy.Infrastructure
{
    public static class NancyModuleExtensions
    {
        public static T BindAndValidateModel<T>(this NancyModule module)
        {
            var result = module.BindAndValidate<T>();
            if (!module.ModelValidationResult.IsValid)
                throw new BadModelException(module.ModelValidationResult);

            return result;
        }
    }
}
