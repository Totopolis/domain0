using Domain0.Service;
using Nancy;
using System;

namespace Domain0.Nancy.Infrastructure
{
    class JwtAuthenticationRequestContext : IRequestContext
    {
        public JwtAuthenticationRequestContext(
            NancyContext nancyContextInstance)
        {
            nancyContext = nancyContextInstance;
        }

        public int UserId
        {
            get
            {
                try
                {
                    return int.Parse(nancyContext.CurrentUser.Identity.Name);
                }
                catch (Exception ex)
                {
                    throw new UnauthorizedAccessException("Unauthorized", ex);
                }
            }
        }

        public string ClientHost => nancyContext.GetClientHost();

        private readonly NancyContext nancyContext;
    }
}
