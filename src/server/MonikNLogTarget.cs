using Monik.Client;
using NLog;
using NLog.Targets;
using System;

namespace Domain0.WinService
{
    [Target("Monik")]
    public class MonikNLogTarget : TargetWithLayout
    {
        public string ConnectionString { get; set; }

        public string QueueName { get; set; }

        public string Source { get; set; }

        public string Instance { get; set; }

        private IClientControl _clientControl;

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

            var sender = new AzureSender(ConnectionString, QueueName);
            var settings = new ClientSettings
            {
                SourceName = Source,
                InstanceName = Instance,
                AutoKeepAliveEnable = true
            };

            _clientControl = new MonikInstance(sender, settings);
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var message = Layout.Render(logEvent);
            if (logEvent.Level == LogLevel.Debug)
            {
                _clientControl.LogicVerbose(message);
            }
            else if (logEvent.Level == LogLevel.Error)
            {
                _clientControl.LogicError(message);
            }
            else if (logEvent.Level == LogLevel.Fatal)
            {
                _clientControl.LogicFatal(message);
            }
            else if (logEvent.Level == LogLevel.Info)
            {
                _clientControl.LogicInfo(message);
            }
            else if (logEvent.Level == LogLevel.Trace)
            {
                _clientControl.LogicVerbose(message);
            }
            else if (logEvent.Level == LogLevel.Warn)
            {
                _clientControl.LogicWarning(message);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            M.OnStop();
        }
    }
}
