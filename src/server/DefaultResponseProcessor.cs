using Nancy.Responses.Negotiation;
using Nancy;
using Nancy.Responses;
using System.Collections.Generic;
using Nancy.Configuration;
using System.Linq;
using System;

namespace Domain0.Nancy.Infrastructure
{
    public class DefaultResponseProcessor : IResponseProcessor
    {
        private readonly ISerializer serializer;
        private readonly INancyEnvironment environment;

        private static readonly IEnumerable<Tuple<string, MediaRange>> extensionMappings =
            new[] { new Tuple<string, MediaRange>("json", new MediaRange("application/json")) };

        public DefaultResponseProcessor(
            IEnumerable<ISerializer> serializers, 
            INancyEnvironment environmentInstance)
        {
            serializer = serializers.FirstOrDefault(x => x.CanSerialize("application/json"));
            environment = environmentInstance;
        }

        public IEnumerable<Tuple<string, MediaRange>> ExtensionMappings => extensionMappings;

        public ProcessorMatch CanProcess(MediaRange mediaRange, dynamic model, NancyContext context)
        {
            return new ProcessorMatch
            {
                ModelResult = MatchResult.DontCare,
                RequestedContentTypeResult = IsSuported(mediaRange)
            };
        }

        private static MatchResult IsSuported(MediaRange mediaRange)
        {
            if (mediaRange.IsWildcard)
                return MatchResult.NoMatch;
            
            return MatchResult.NonExactMatch;
        }

        public Response Process(MediaRange mediaRange, dynamic model, NancyContext context)
            => new JsonResponse((object) model, serializer, environment);
    }
}
