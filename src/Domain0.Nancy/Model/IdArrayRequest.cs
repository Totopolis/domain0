using System.Collections.Generic;
using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Model
{
    [Model("IdArrayRequest")]
    public class IdArrayRequest
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<IdArrayRequest> DefaultDescriptor
            => MessageDescriptor<IdArrayRequest>.Create(new[]
            {
                FieldSetting<IdArrayRequest>.CreateInt32Array(
                    1, 
                    c => c.Ids, 
                    (c, v) => c.Ids.Add(v), 
                    c => c.Ids?.Count > 0),
            });

        public List<int> Ids { get; set; }
    }
}
