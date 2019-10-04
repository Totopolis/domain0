using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;
using System.Collections.Generic;

namespace Domain0.Model
{
    [Model("User role")]
    public class UserRole
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<UserRole> DefaultDescriptor
            => MessageDescriptor<UserRole>.Create(new[]
            {
                FieldSetting<UserRole>.CreateInt32(1,
                    c => c.Id.Value,
                    (c, v) => c.Id = v,
                    c => c.Id.HasValue),
                FieldSetting<UserRole>.CreateString(2,
                    c => c.Name,
                    (c, v) => c.Name = v,
                    c => c.Name?.Length > 0),
                FieldSetting<UserRole>.CreateString(3,
                    c => c.Description,
                    (c, v) => c.Description = v),
                FieldSetting<UserRole>.CreateBool(4,
                    c => c.IsDefault,
                    (c, v) => c.IsDefault = v),
                FieldSetting<UserRole>.CreateInt32(1,
                    c => c.UserId,
                    (c, v) => c.UserId = v),
            });

        public int? Id { get; set; }

        public int UserId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsDefault { get; set; }
    }

    [Model("Role by user filter")]
    public class RoleUserFilter
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<RoleUserFilter> DefaultDescriptor
            => MessageDescriptor<RoleUserFilter>.Create(new[]
            {
                FieldSetting<RoleUserFilter>.CreateInt32Array(
                    1,
                    c       => c.UserIds,
                    (c, v)  => c.UserIds.Add(v),
                    c       => c.UserIds?.Count > 0)
            });

        public RoleUserFilter()
        {
        }

        public RoleUserFilter(int id)
        {
            UserIds.Add(id);
        }

        public RoleUserFilter(IEnumerable<int> ids)
        {
            UserIds.AddRange(ids);
        }

        public List<int> UserIds { get; set; } = new List<int>();
    }
}
