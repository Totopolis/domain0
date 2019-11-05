using System;
using Monik.Client;
using Monik.Common;
using NLog;
using NLog.Targets;

namespace Domain0.WinService
{
    [Target("Monik")]
    public class MonikNLogTarget : TargetWithLayout
    {
        public string ConnectionString { get; set; }

        public string QueueName { get; set; }

        public string Source { get; set; }

        public string Instance { get; set; }

        private IMonik monikClient;

        public MonikNLogTarget()
        {
            Source = string.IsNullOrEmpty(Source) ? "Domain0" : Source;
            Instance = string.IsNullOrEmpty(Instance) ? "Dev" : Instance;
        }

        protected override void InitializeTarget()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new ArgumentNullException(nameof(ConnectionString));
            if (string.IsNullOrEmpty(QueueName))
                throw new ArgumentNullException(nameof(QueueName));

            var sender = new RabbitMqSender(ConnectionString, QueueName);
            var settings = new ClientSettings
            {
                SourceName = Source,
                InstanceName = Instance,
                AutoKeepAliveEnable = true
            };

            monikClient = new MonikClient(sender, settings);
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var message = Layout.Render(logEvent);
            if (logEvent.Level == LogLevel.Debug)
            {
                monikClient.ApplicationVerbose(message);
            }
            else if (logEvent.Level == LogLevel.Error)
            {
                monikClient.LogicError(message);
            }
            else if (logEvent.Level == LogLevel.Fatal)
            {
                monikClient.LogicFatal(message);
            }
            else if (logEvent.Level == LogLevel.Info)
            {
                monikClient.LogicInfo(message);
            }
            else if (logEvent.Level == LogLevel.Trace)
            {
                monikClient.LogicVerbose(message);
            }
            else if (logEvent.Level == LogLevel.Warn)
            {
                monikClient.LogicWarning(message);
            }
        }

        protected override void CloseTarget()
        {
            monikClient?.OnStop();
            base.CloseTarget();
        }
    }
}
