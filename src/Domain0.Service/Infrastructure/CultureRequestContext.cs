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
            CultureContextSettings settings)
        {
            nancyContext = nancyContextInstance;
            Culture = !string.IsNullOrEmpty(settings?.DefaultCulture) &&
                      !nancyContext.Request.Headers.AcceptLanguage.Any()
                ? CultureInfo.GetCultureInfo(settings.DefaultCulture)
                : nancyContext.Culture;
        }

        public CultureInfo Culture { get; set; }

        private readonly NancyContext nancyContext;
    }
}