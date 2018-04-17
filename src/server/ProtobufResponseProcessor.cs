using Nancy.Responses.Negotiation;
using System;
using System.Collections.Generic;
using Nancy;

namespace Domain0.Nancy.Infrastructure
{
    public class ProtobufResponseProcessor : IResponseProcessor
    {
        private static readonly IEnumerable<Tuple<string, MediaRange>> extensionMappings =
            new[] {new Tuple<string, MediaRange>("protobuf", new MediaRange(ProtobufResponse.ContentType_Protobuf))};

        public IEnumerable<Tuple<string, MediaRange>> ExtensionMappings => extensionMappings;

        public ProcessorMatch CanProcess(MediaRange mediaRange, dynamic model, NancyContext context)
            => new ProcessorMatch
            {
                ModelResult = MatchResult.DontCare,
                RequestedContentTypeResult = mediaRange.Matches(ProtobufResponse.ContentType_Protobuf)
                    ? MatchResult.ExactMatch
                    : MatchResult.NoMatch
            };

        public Response Process(MediaRange mediaRange, dynamic model, NancyContext context)
            => new ProtobufResponse((object) model);
    }
}
