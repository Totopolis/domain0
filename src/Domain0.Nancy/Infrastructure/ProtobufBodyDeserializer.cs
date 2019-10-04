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
            var descriptor = ProtobufResponse.GetDescriptor(context.DestinationType);
            var contentLength = (int) context.Context.Request.Headers.ContentLength;
            var bytes = new BinaryReader(bodyStream).ReadBytes(contentLength);

            var result = descriptor.Read(bytes);
            if (result is SimpleValue simple)
                result = simple.Obj;

            return result;
        }
    }
}
