using Nancy;
using Nancy.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Domain0.Nancy.Infrastructure
{
    public static class EmbeddedContentConventionBuilderExtensions
    {
        public static void AddEmbeddedDirectory(this IList<Func<NancyContext, string, Response>> conventions,
            string requestPath, string embeddedPath)
            => conventions.Add(EmbeddedContentConventionBuilder.AddEmbeddedDirectory(requestPath, embeddedPath));
    }

    public static class EmbeddedContentConventionBuilder
    {
        public static Func<NancyContext, string, Response> AddEmbeddedDirectory(string requestPath, string embedDirectory)
        {
            return (ctx, root) =>
            {
                var path = ctx.Request.Url.Path;
                if (!path.StartsWith(requestPath))
                    return null;

                var assembly = Assembly.GetExecutingAssembly();
                var filename = Path.GetFileName(ctx.Request.Url.Path);
                if (string.IsNullOrEmpty(filename))
                    return ctx.Response.WithStatusCode(HttpStatusCode.NotFound);

                var pathParts = string.Concat(embedDirectory, path.Substring(requestPath.Length)).Split('/');
                if (pathParts.Length == 0)
                    return ctx.Response.WithStatusCode(HttpStatusCode.NotFound);

                var embeddedPath = GetEmbeddedPath(assembly, pathParts);
                var stream = assembly.GetManifestResourceStream(embeddedPath);
                if (stream == null)
                    return ctx.Response.WithStatusCode(HttpStatusCode.NotFound);

                return new StreamResponse(() => stream, MimeTypes.GetMimeType(filename));
            };
        }

        private static string GetEmbeddedPath(Assembly assembly, params string[] parts)
        {
            var path = string.Join(".", parts.Take(parts.Length - 1).Select(p => p.Replace("-", "_")));
            if (!string.IsNullOrEmpty(path))
                return $"{assembly.GetName().Name}.{path}.{parts.Last()}";

            return $"{assembly.GetName().Name}.{parts.Last()}";
        }
    }
}
