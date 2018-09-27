using System.Collections.Generic;
using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Model
{
    [Model("Role permission")]
    public class RolePermission
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<RolePermission> DefaultDescriptor
            => MessageDescriptor<RolePermission>.Create(new[]
            {
                FieldSetting<RolePermission>.CreateInt32(1,  
                    c => c.Id.Value,
                    (c, v) => c.Id = v,
                    c => c.Id.HasValue),
                FieldSetting<RolePermission>.CreateInt32(2,
                    c => c.ApplicationId, 
                    (c, v) => c.ApplicationId = v),
                FieldSetting<RolePermission>.CreateString(3, 
                    c => c.Name,
                    (c, v) => c.Name = v,
                    c => c.Name?.Length > 0),
                FieldSetting<RolePermission>.CreateString(4,
                    c => c.Description,
                    (c, v) => c.Description = v),
                FieldSetting<RolePermission>.CreateInt32(5,
                    c => c.RoleId.Value,
                    (c, v) => c.RoleId = v),
            });

        public int? Id { get; set; }

        public int? RoleId { get; set; }

        public int ApplicationId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }        
    }


    [Model("Role permission filter")]
    public class RolePermissionFilter
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<RolePermissionFilter> DefaultDescriptor
            => MessageDescriptor<RolePermissionFilter>.Create(new[]
            {
                FieldSetting<RolePermissionFilter>.CreateInt32Array(
                    1,
                    c       => c.RoleIds,
                    (c, v)  => c.RoleIds.Add(v),
                    c       => c.RoleIds?.Count > 0),
            });

        public RolePermissionFilter()
        {
        }

        public RolePermissionFilter(int id)
        {
            RoleIds.Add(id);
        }

        public RolePermissionFilter(IEnumerable<int> ids)
        {
            RoleIds.AddRange(ids);
        }

        public List<int> RoleIds { get; set; } = new List<int>();
    }
}
