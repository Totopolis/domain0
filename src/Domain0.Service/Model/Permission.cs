using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;
using System.Collections.Generic;

namespace Domain0.Model
{
    [Model("Permission")]
    public class Permission
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<Permission> DefaultDescriptor
            => MessageDescriptor<Permission>.Create(new[]
            {
                FieldSetting<Permission>.CreateInt32(1,  
                    c => c.Id.Value,
                    (c, v) => c.Id = v,
                    c => c.Id.HasValue),
                FieldSetting<Permission>.CreateInt32(2,
                    c => c.ApplicationId, 
                    (c, v) => c.ApplicationId = v),
                FieldSetting<Permission>.CreateString(3, 
                    c => c.Name,
                    (c, v) => c.Name = v,
                    c => c.Name?.Length > 0),
                FieldSetting<Permission>.CreateString(4,
                    c => c.Description,
                    (c, v) => c.Description = v),
            });

        /// <summary>
        /// Идентификатор
        /// </summary>
        public int? Id { get; set; }
        /// <summary>
        /// Идентификатор приложения
        /// </summary>
        public int ApplicationId { get; set; }
        /// <summary>
        /// Имя
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Описание
        /// </summary>
        public string Description { get; set; }        
    }

    [Model("Permission filter")]
    public class PermissionFilter
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<PermissionFilter> DefaultDescriptor
            => MessageDescriptor<PermissionFilter>.Create(new[]
            {
                FieldSetting<PermissionFilter>.CreateInt32Array(
                    1, 
                    c       => c.PermissionIds, 
                    (c, v)  => c.PermissionIds.Add(v), 
                    c       => c.PermissionIds?.Count > 0)
            });

        public PermissionFilter()
        {
        }

        public PermissionFilter(int id)
        {
            PermissionIds.Add(id);
        }

        public PermissionFilter(IEnumerable<int> ids)
        {
            PermissionIds.AddRange(ids);
        }

        public List<int> PermissionIds { get; set; } = new List<int>();
    }
}
