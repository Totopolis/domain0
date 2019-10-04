namespace Domain0.Service
{
    public class ThresholdSettings
    {
        public int CacheLimitMB { get; internal set; }
        public int HourlyRequestsLimitByActionByIP { get; internal set; }
        public int MinuteRequestsLimitByActionByIP { get; internal set; }
    }
}
