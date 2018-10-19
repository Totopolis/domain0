using Nancy;
using Nancy.Responses.Negotiation;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Domain0.Nancy.Infrastructure
{
    public class JsonNetSerializer : ISerializer
    {
        private readonly JsonSerializer _serializer;

        public JsonNetSerializer()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver()
            };

            _serializer = JsonSerializer.Create(settings);
        }

        public IEnumerable<string> Extensions => Enumerable.Empty<string>();

        public bool CanSerialize(MediaRange mediaRange)
        {
            return mediaRange.Type == "application"
                && mediaRange.Subtype == "json";
        }


        public void Serialize<TModel>(MediaRange mediaRange, TModel model, Stream outputStream)
        {
            using (var writer = new JsonTextWriter(new StreamWriter(outputStream)))
            {
                _serializer.Serialize(writer, model);
                writer.Flush();
            }
        }
    }
}