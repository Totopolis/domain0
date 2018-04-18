using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;
using System.Collections.Generic;

namespace Domain0.Model
{
    public class UserProfileFilter
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<UserProfileFilter> DefaultDescriptor
            => MessageDescriptor<UserProfileFilter>.Create(new[]
            {
                FieldSetting<UserProfileFilter>.CreateInt32Array(1, c => c.UserIds, (c, v) => c.UserIds.Add(v), c => c.UserIds?.Count > 0)
            });

        /// <summary>
        /// Идентификаторы пользователей.
        /// </summary>
        public List<int> UserIds { get; set; }
    }
}
