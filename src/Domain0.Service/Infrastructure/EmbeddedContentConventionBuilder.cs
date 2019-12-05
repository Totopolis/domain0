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
        public static void AddEmbeddedDirectory<T>(this IList<Func<NancyContext, string, Response>> conventions,
            string requestPath, string embeddedPath)
            => conventions.Add(EmbeddedContentConventionBuilder.AddEmbeddedDirectory<T>(requestPath, embeddedPath));
    }

    public static class EmbeddedContentConventionBuilder
    {
        public static Func<NancyContext, string, Response> AddEmbeddedDirectory<T>(string requestPath, string embedDirectory)
        {
            return (ctx, root) =>
            {
                var path = ctx.Request.Url.Path;
                if (!path.StartsWith(requestPath))
                    return null;

                var assembly = Assembly.GetExecutingAssembly();
                var filename = Path.GetFileName(ctx.Request.Url.Path);
                if (string.IsNullOrEmpty(filename))
                    return HttpStatusCode.NotFound;

                var pathParts = string.Concat(embedDirectory, path.Substring(requestPath.Length)).Split('/');
                if (pathParts.Length == 0)
                    return HttpStatusCode.NotFound;

                var embeddedPath = GetEmbeddedPath<T>(pathParts);
                var stream = assembly.GetManifestResourceStream(embeddedPath);
                if (stream == null)
                    return HttpStatusCode.NotFound;

                return new StreamResponse(() => stream, MimeTypes.GetMimeType(filename));
            };
        }

        private static string GetEmbeddedPath<T>(params string[] parts)
        {
            var path = string.Join(".", parts.Take(parts.Length - 1).Select(p => p.Replace("-", "_")));
            if (!string.IsNullOrEmpty(path))
                return $"{typeof(T).Namespace}.{path}.{parts.Last()}";

            return $"{typeof(T).Namespace}.{parts.Last()}";
        }
    }
}
