using System.IO;
using Nancy;
using Nancy.ErrorHandling;

namespace Domain0.Nancy.Infrastructure
{
    public class StatusCodeHandler : IStatusCodeHandler
    {
        public StatusCodeHandler(IRootPathProvider rootPathProvider)
        {
        }

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            return true;
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context)
        {
            //I have nothing to add
        }
    }
}