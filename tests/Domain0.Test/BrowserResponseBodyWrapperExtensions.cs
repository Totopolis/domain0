using System;
using System.IO;
using System.Linq;
using Domain0.Nancy.Infrastructure;
using Nancy.Testing;
using Newtonsoft.Json;

namespace Domain0.Test
{
    public enum DataFormat
    {
        Json,
        Proto
    }

    public static class BrowserResponseBodyWrapperExtensions
    {
        public static T AsDataFormat<T>(this BrowserResponseBodyWrapper body, DataFormat format)
        {
            switch (format)
            {
                case DataFormat.Json:
                    return JsonConvert.DeserializeObject<T>(body.AsString());
                case DataFormat.Proto:
                    var descriptor = ProtobufResponse.GetDescriptor(typeof(T));
                    var stream = new MemoryStream();
                    body.AsStream().CopyTo(stream);
                    object result = null;
                    if (typeof(T).IsArray)
                        result = descriptor.ReadLenDelimitedStream(stream.ToArray());
                    else
                        result = descriptor.Read(stream.ToArray());

                    switch (result)
                    {
                        case T model:
                            return model;
                        case SimpleValue<T> simple:
                            return simple.Value;
                        default:
                            throw new NotImplementedException();
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public static T[] AsArrayDataFormat<T>(this BrowserResponseBodyWrapper body, DataFormat format)
        {
            switch (format)
            {
                case DataFormat.Json:
                    return JsonConvert.DeserializeObject<T[]>(body.AsString());
                case DataFormat.Proto:
                    var descriptor = ProtobufResponse.GetDescriptor(typeof(T));
                    var stream = new MemoryStream();
                    body.AsStream().CopyTo(stream);
                    return descriptor.ReadLenDelimitedStream(stream.ToArray()).Cast<T>().ToArray();
                default:
                    throw new NotImplementedException();
            }
        }

        public static void Accept(this BrowserContext browserContext, DataFormat format)
        {
            switch (format)
            {
                case DataFormat.Json:
                    browserContext.Accept("application/json");
                    break;
                case DataFormat.Proto:
                    browserContext.Accept("application/x-protobuf");
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public static void DataFormatBody<T>(this BrowserContext browserContext, DataFormat format, T model)
        {
            switch (format)
            {
                case DataFormat.Json:
                    browserContext.JsonBody(model);
                    break;
                case DataFormat.Proto:
                    var descriptor = ProtobufResponse.GetDescriptor(typeof(T));
                    object value = SimpleValue.FromValue(model);
                    if (value == null)
                        value = model;

                    var bytes = descriptor.Write(value);
                    browserContext.Body(new MemoryStream(bytes), "application/x-protobuf");
                    browserContext.Header("Content-Length", bytes.Length.ToString());
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}