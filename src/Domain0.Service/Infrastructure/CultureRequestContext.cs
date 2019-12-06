using System.Globalization;
using System.Linq;
using Domain0.Service;
using Nancy;

namespace Domain0.Nancy.Infrastructure
{
    class CultureRequestContext : ICultureRequestContext
    {
        public CultureRequestContext(
            NancyContext nancyContextInstance,
            string defaultCulture)
        {
            nancyContext = nancyContextInstance;
            Culture = !string.IsNullOrEmpty(defaultCulture) &&
                      !nancyContext.Request.Headers.AcceptLanguage.Any()
                ? CultureInfo.GetCultureInfo(defaultCulture)
                : nancyContext.Culture;
        }

        public CultureInfo Culture { get; set; }

        private readonly NancyContext nancyContext;
    }
}