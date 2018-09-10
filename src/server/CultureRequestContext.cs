using Domain0.Service;
using Nancy;
using System.Globalization;

namespace Domain0.Nancy.Infrastructure
{
    class CultureRequestContext : ICultureRequestContext
    {
        public CultureRequestContext(
            NancyContext nancyContextInstance)
        {
            nancyContext = nancyContextInstance;
        }

        public CultureInfo Culture => nancyContext.Culture;

        private readonly NancyContext nancyContext;
    }
}
