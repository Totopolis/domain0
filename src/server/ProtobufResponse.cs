using Nancy;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace Domain0.Nancy.Infrastructure
{
    public class ProtobufResponse : Response
    {
        public static ConcurrentDictionary<Type, Action<Stream, object>> Cache = new ConcurrentDictionary<Type, Action<Stream, object>>();

        public const string ContentType_Protobuf = "application/x-protobuf";

        public ProtobufResponse(object model)
        {
            ContentType = ContentType_Protobuf;
            StatusCode = HttpStatusCode.OK;
            Contents = model == null ? NoBody : stream =>
            {
                var serialize = Cache.GetOrAdd(model.GetType(), type => (str, obj) =>
                {
                    var bytes = obj as byte[];
                    if (bytes != null)
                        str.Write(bytes, 0, bytes.Length);
                });
                if (serialize == null)
                    return;

                serialize(stream, model);
            };
        }
    }
}
