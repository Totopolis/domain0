namespace Domain0.Service
{
    public class ThresholdSettings
    {
        public int CacheLimitMB { get; set; }
        public int HourlyRequestsLimitByActionByIP { get; set; }
        public int MinuteRequestsLimitByActionByIP { get; set; }
    }
}
