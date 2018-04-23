using Gerakul.ProtoBufSerializer;
using Nancy;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Domain0.Nancy.Infrastructure
{
    public class SimpleValue
    {
        public virtual object Obj { get; }

        public static SimpleValue FromValue(object value)
        {
            if (!(value is ValueType) && !(value is string))
                return null;

            switch (value)
            {
                case string strModel:
                    return new SimpleValue<string> {Value = strModel};
                case bool boolModel:
                    return new SimpleValue<bool> {Value = boolModel};
                case int int32Model:
                    return new SimpleValue<int> {Value = int32Model};
                case long int64Model:
                    return new SimpleValue<long> {Value = int64Model};
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public class SimpleValue<T> : SimpleValue
    {
        public T Value { get; set; }

        public override object Obj { get => Value; }
    }

    public static class SimpleValueDescriptors
    {
        public static MessageDescriptor<SimpleValue<string>> StringDescriptor
            => MessageDescriptor<SimpleValue<string>>.Create(
                new[] { FieldSetting<SimpleValue<string>>.CreateString(1, c => c.Value, (c, v) => c.Value = v, c => c.Value?.Length > 0) }
            );

        public static MessageDescriptor<SimpleValue<int>> Int32Descriptor
            => MessageDescriptor<SimpleValue<int>>.Create(
                new[] {FieldSetting<SimpleValue<int>>.CreateInt32(1, c => c.Value, (c, v) => c.Value = v)}
            );

        public static MessageDescriptor<SimpleValue<long>> Int64Descriptor
            => MessageDescriptor<SimpleValue<long>>.Create(
                new[] {FieldSetting<SimpleValue<long>>.CreateInt64(1, c => c.Value, (c, v) => c.Value = v)}
            );

        public static MessageDescriptor<SimpleValue<bool>> BooleanDescriptor
            => MessageDescriptor<SimpleValue<bool>>.Create(
                new[] {FieldSetting<SimpleValue<bool>>.CreateBool(1, c => c.Value, (c, v) => c.Value = v)}
            );
    }

    public class ProtobufResponse : Response
    {
        public static ConcurrentDictionary<Type, IUntypedMessageDescriptor> Cache = new ConcurrentDictionary<Type, IUntypedMessageDescriptor>();

        public static IUntypedMessageDescriptor GetDescriptor(Type type)
        {
            return Cache.GetOrAdd(type, t =>
            {
                if (type.IsArray)
                    t = type.GetElementType();
                else 
                if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
                    t = type.GenericTypeArguments.FirstOrDefault();

                PropertyInfo descriptorProperty = null;
                if (t.IsValueType || type == typeof(string))
                {
                    var descriptorProperties = typeof(SimpleValueDescriptors)
                        .GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .Where(p => typeof(IUntypedMessageDescriptor).IsAssignableFrom(p.PropertyType));

                    descriptorProperty = descriptorProperties.FirstOrDefault(p => p.PropertyType.GenericTypeArguments[0].GenericTypeArguments[0] == t);
                }
                else
                {
                    descriptorProperty = t
                        .GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .FirstOrDefault(p => typeof(IUntypedMessageDescriptor).IsAssignableFrom(p.PropertyType));
                }

                var descriptor = (IUntypedMessageDescriptor) descriptorProperty?.GetValue(null);
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

                byte[] bytes;
                if (model is IEnumerable collection)
                {
                    bytes = descriptor.WriteLenDelimitedStream(collection);
                }
                else 
                {
                    var simpleModel = SimpleValue.FromValue(model);
                    if (simpleModel != null)
                        bytes = descriptor.Write(simpleModel);
                    else
                        bytes = descriptor.Write(model);
                }

                stream.Write(bytes, 0, bytes.Length);
            });
        }
    }
}
