using Nancy.ModelBinding;
using Nancy.Responses.Negotiation;
using System.IO;

namespace Domain0.Nancy.Infrastructure
{
    public class ProtobufBodyDeserializer : IBodyDeserializer
    {
        public bool CanDeserialize(MediaRange mediaRange, BindingContext context)
            => mediaRange == ProtobufResponse.ContentType_Protobuf;

        public object Deserialize(MediaRange mediaRange, Stream bodyStream, BindingContext context)
        {
            return null;
        }
    }
}
