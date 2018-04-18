using Gerakul.ProtoBufSerializer;
using Nancy;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Domain0.Nancy.Infrastructure
{
    public class ProtobufResponse : Response
    {
        public static ConcurrentDictionary<Type, IUntypedMessageDescriptor> Cache = new ConcurrentDictionary<Type, IUntypedMessageDescriptor>();

        public static IUntypedMessageDescriptor GetDescriptor(Type type)
        {
            return Cache.GetOrAdd(type, t =>
            {
                var descriptorProperty = t.GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    .FirstOrDefault(p => typeof(IUntypedMessageDescriptor).IsAssignableFrom(p.PropertyType));
                var descriptor = (IUntypedMessageDescriptor)descriptorProperty?.GetValue(null);
                if (descriptor == null)
                    throw new ArgumentNullException(nameof(descriptor), $"Descriptor not found in {t}");
                return descriptor;
            });
        }

        public const string ContentType_Protobuf = "application/x-protobuf";

        public ProtobufResponse(object model)
        {
            ContentType = ContentType_Protobuf;
            StatusCode = HttpStatusCode.OK;
            Contents = model == null ? NoBody : (stream =>
            {
                var descriptor = GetDescriptor(model.GetType());
                var bytes = descriptor.Write(model);
                stream.Write(bytes, 0, bytes.Length);
            });
        }
    }
}
