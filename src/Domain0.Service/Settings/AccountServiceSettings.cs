using System;

namespace Domain0.Service
{
    public class AccountServiceSettings
    {
        public TimeSpan MessagesResendCooldown { get; set; }

        public TimeSpan PinExpirationTime { get; set; }

        public TimeSpan EmailCodeExpirationTime { get; set; }
    }
}