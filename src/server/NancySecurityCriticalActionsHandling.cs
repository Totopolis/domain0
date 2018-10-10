using Autofac;
using Domain0.Exceptions;
using Nancy.Bootstrapper;
using NLog;

namespace Domain0.Nancy.Infrastructure
{
    public class NancySecurityCriticalActionsHandling
    {
        public static void Enable(
            ILifetimeScope requestContainer,
            IPipelines pipelines)
        {
            var logger = requestContainer.Resolve<ILogger>();

            pipelines.OnError.AddItemToStartOfPipeline((ctx, ex) =>
            {
                try
                {
                    switch (ex)
                    {
                        case TokenSecurityException _:
                            logger.Warn($"Wrong token from host: {ctx.Request?.UserHostAddress} to path: {ctx.Request?.Path}");
                            break;
                    }
                }
                catch
                {
                    // ignored because useless
                }

                return null;
            });
        }
    }
}