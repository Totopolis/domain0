using System;
using System.Linq;
using Nancy;
using Domain0.Exceptions;
using Nancy.Bootstrapper;
using Nancy.ModelBinding;
using System.IO;
using System.Text;
using System.Threading.Tasks;

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

        private const string AllowOriginKey = "Access-Control-Allow-Origin";

        private const string AllowMethodsKey = "Access-Control-Allow-Methods";

        private const string RequestHeadersKey = "Access-Control-Request-Headers";

        private const string AllowHeadersKey = "Access-Control-Allow-Headers";

        private const string AllowCredentialsKey = "Access-Control-Allow-Credentials";

        private const string OriginKey = "Origin";

        public static void EnableCors(this IPipelines pipelines)
        {
            pipelines.AfterRequest.AddItemToEndOfPipeline(ctx =>
            {
                if (!ctx.Request.Headers.Keys.Contains(OriginKey))
                    return;

                var origins = string.Join(" ", ctx.Request.Headers[OriginKey]);
                ctx.Response.Headers[AllowOriginKey] = origins;

                if (string.Compare(ctx.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase) != 0)
                    return;

                // handle CORS preflight request
                ctx.Response.Headers[AllowMethodsKey] = "GET, POST, PUT, DELETE, OPTIONS";

                ctx.Response.Headers.Add(AllowCredentialsKey, "true");

                if (!ctx.Request.Headers.Keys.Contains(RequestHeadersKey))
                    return;

                var allowedHeaders = string.Join(", ", ctx.Request.Headers[RequestHeadersKey]);
                ctx.Response.Headers[AllowHeadersKey] = allowedHeaders;
            });
        }

        public static async Task<string> AsString(this Stream stream)
        {
            using (var sr = new StreamReader(stream, Encoding.UTF8, true, 4096, true))
            {
                var result = string.Empty;

                if (!stream.CanSeek)
                    return result;

                var initial = stream.Position;
                try
                {
                    stream.Position = 0;
                    result = await sr.ReadToEndAsync();
                }
                finally
                {
                    stream.Position = initial;
                }

                return result;
            }
        }

        public static string GetClientHost(this NancyContext context)
        {
            if (context.Request == null)
                return string.Empty;

            if (context.Request.Headers["X-Real-IP"].Any())
            {
                return string.Join(", ", context.Request.Headers["X-Real-IP"]);
            }

            return context.Request.UserHostAddress;
        }
    }
}
