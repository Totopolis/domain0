using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;
using System.Collections.Generic;

namespace Domain0.Model
{
    [Model("Environment")]
    public class Environment
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<Environment> DefaultDescriptor
            => MessageDescriptor<Environment>.Create(new[]
            {
                FieldSetting<Environment>.CreateInt32(1,
                    c => c.Id.Value,
                    (c, v) => c.Id = v,
                    c => c.Id.HasValue),
                FieldSetting<Environment>.CreateString(2,
                    c => c.Name,
                    (c, v) => c.Name = v,
                    c => c.Name?.Length > 0),
                FieldSetting<Environment>.CreateString(3,
                    c => c.Description,
                    (c, v) => c.Description = v),
                FieldSetting<Environment>.CreateString(4,
                    c => c.Token,
                    (c, v) => c.Token = v),
                FieldSetting<Environment>.CreateBool(5,
                    c => c.IsDefault,
                    (c, v) => c.IsDefault = v),
            });

        public int? Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Token { get; set; }

        public bool IsDefault { get; set; }
    }

    [Model("Environment filter")]
    public class EnvironmentFilter
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<EnvironmentFilter> DefaultDescriptor
            => MessageDescriptor<EnvironmentFilter>.Create(new[]
            {
                FieldSetting<EnvironmentFilter>.CreateInt32Array(
                    1,
                    f       => f.Ids,
                    (f, v)  => f.Ids.Add(v),
                    f       => f.Ids?.Count > 0),
                FieldSetting<EnvironmentFilter>.CreateBool(
                    2,
                    f       => f.LoadAll.Value,
                    (f, v)  => f.LoadAll = v,
                    f      => f.LoadAll.HasValue)
            });

        public EnvironmentFilter() { }

        public EnvironmentFilter(bool loadAll) { LoadAll = loadAll; }

        public EnvironmentFilter(int id) { Ids.Add(id); }

        public EnvironmentFilter(IEnumerable<int> ids) { Ids.AddRange(ids); }

        public List<int> Ids { get; set; } = new List<int>();

        public bool? LoadAll { get; set; }
    }
}
