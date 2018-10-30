using Gerakul.ProtoBufSerializer;
using Nancy.Swagger.Annotations.Attributes;

namespace Domain0.Model
{
    [Model("User profile")]
    public class UserProfile
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<UserProfile> DefaultDescriptor
            => MessageDescriptor<UserProfile>.Create(new[]
            {
                FieldSetting<UserProfile>.CreateInt32(1, c => c.Id, (c, v) => c.Id = v),
                FieldSetting<UserProfile>.CreateString(2, c => c.Name, (c, v) => c.Name = v, c => c.Name?.Length > 0),
                FieldSetting<UserProfile>.CreateString(3, c => c.Phone, (c, v) => c.Phone = v, c => !string.IsNullOrWhiteSpace(c.Phone)),
                FieldSetting<UserProfile>.CreateString(4, c => c.Description, (c, v) => c.Description = v, c => c.Description?.Length > 0),
                FieldSetting<UserProfile>.CreateString(5, c => c.Email, (c, v) => c.Email = v, c => !string.IsNullOrWhiteSpace(c.Email)),
                FieldSetting<UserProfile>.CreateBool(6, c => c.IsLocked, (c, v) => c.IsLocked= v),
            });

        /// <summary>
        /// User identifier
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// User name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// User phone number
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// User email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// If user is temprorary locked
        /// </summary>
        public bool IsLocked { get; set; }
    }
}
