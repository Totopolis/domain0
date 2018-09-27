using System.Collections.Generic;
using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Model
{
    [Model("User permission")]
    public class UserPermission
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<UserPermission> DefaultDescriptor
            => MessageDescriptor<UserPermission>.Create(new[]
            {
                FieldSetting<UserPermission>.CreateInt32(1,  
                    c => c.Id.Value,
                    (c, v) => c.Id = v,
                    c => c.Id.HasValue),
                FieldSetting<UserPermission>.CreateInt32(2,
                    c => c.ApplicationId, 
                    (c, v) => c.ApplicationId = v),
                FieldSetting<UserPermission>.CreateString(3, 
                    c => c.Name,
                    (c, v) => c.Name = v,
                    c => c.Name?.Length > 0),
                FieldSetting<UserPermission>.CreateString(4,
                    c => c.Description,
                    (c, v) => c.Description = v),
                FieldSetting<UserPermission>.CreateInt32(5,
                    c => c.UserId.Value,
                    (c, v) => c.UserId = v),
                FieldSetting<UserPermission>.CreateInt32(6,
                    c => c.RoleId.Value,
                    (c, v) => c.RoleId = v),
            });

        public int? Id { get; set; }

        public int? RoleId { get; set; }

        public int? UserId { get; set; }

        public int ApplicationId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }        
    }

    [Model("User permission filter")]
    public class UserPermissionFilter
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<UserPermissionFilter> DefaultDescriptor
            => MessageDescriptor<UserPermissionFilter>.Create(new[]
            {
                FieldSetting<UserPermissionFilter>.CreateInt32Array(
                    1,
                    c       => c.UserIds,
                    (c, v)  => c.UserIds.Add(v),
                    c       => c.UserIds?.Count > 0),
            });

        public UserPermissionFilter()
        {
        }

        public UserPermissionFilter(int id)
        {
            UserIds.Add(id);
        }

        public UserPermissionFilter(IEnumerable<int> ids)
        {
            UserIds.AddRange(ids);
        }

        public List<int> UserIds { get; set; } = new List<int>();
    }
}
