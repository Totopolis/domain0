using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;
using System.Collections.Generic;

namespace Domain0.Model
{
    [Model("Role")]
    public class Role
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<Role> DefaultDescriptor
            => MessageDescriptor<Role>.Create(new[]
            {
                FieldSetting<Role>.CreateInt32(1,
                    c => c.Id.Value,
                    (c, v) => c.Id = v,
                    c => c.Id.HasValue),
                FieldSetting<Role>.CreateString(2,
                    c => c.Name,
                    (c, v) => c.Name = v,
                    c => c.Name?.Length > 0),
                FieldSetting<Role>.CreateString(3,
                    c => c.Description,
                    (c, v) => c.Description = v),
                FieldSetting<Role>.CreateBool(4,
                    c => c.IsDefault,
                    (c, v) => c.IsDefault = v),
            });

        /// <summary>
        /// Идентификатор
        /// </summary>
        public int? Id { get; set; }
        /// <summary>
        /// Имя
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Описание
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Роль по умолчанию
        /// </summary>
        public bool IsDefault { get; set; }
    }

    [Model("Role filter")]
    public class RoleFilter
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<RoleFilter> DefaultDescriptor
            => MessageDescriptor<RoleFilter>.Create(new[]
            {
                FieldSetting<RoleFilter>.CreateInt32Array(
                    1,
                    c       => c.RoleIds,
                    (c, v)  => c.RoleIds.Add(v),
                    c       => c.RoleIds?.Count > 0)
            });

        public RoleFilter()
        {
        }

        public RoleFilter(int id)
        {
            RoleIds.Add(id);
        }

        public RoleFilter(IEnumerable<int> ids)
        {
            RoleIds.AddRange(ids);
        }

        /// <summary>
        /// Идентификаторы шаблонов.
        /// </summary>
        public List<int> RoleIds { get; set; } = new List<int>();
    }
}
