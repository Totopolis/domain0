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
            Culture = nancyContext.Culture; 
        }

        public CultureInfo Culture { get; set; }

        private readonly NancyContext nancyContext;
    }
}
