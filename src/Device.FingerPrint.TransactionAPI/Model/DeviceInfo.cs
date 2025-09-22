namespace Device.FingerPrint.Models
{
    public class DeviceInfo
    {
        public string NA { get; set; }

        public string DeviceCode { get; set; }

        public string AppVersion { get; set; }

        public string TimeZone { get; set; }

        public string UserAgent { get; set; }

        public DeviceScreen Screen { get; set; }

        public string DeviceId { get; set; }

        public string HRef { get; set; }

        public long? CapturedDate { get; set; }

        public string Version { get; set; }

        public BotD BotD { get; set; }
    }
}
