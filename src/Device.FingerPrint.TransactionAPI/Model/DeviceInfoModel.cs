namespace Device.FingerPrint.Model
{
    public class DeviceInfoModel
    {
        public string OSName { get; set; }

        public string DeviceFPCode { get; set; }

        public string DeviceNo { get; set; }

        public string NA { get; set; }

        public string AppVersion { get; set; }

        public string TimeZone { get; set; }

        public string UserAgent { get; set; }

        public ScreenInfoModel Screen { get; set; }

        public string HRef { get; set; }

        public long CapturedDate { get; set; }

        public string Battery { get; set; }

        public string Ip { get; set; }

        public string LocalIps { get; set; }

        public bool IsNonManilaCSD { get; set; }

        public bool InvalidDeviceId { get; set; }

        public string Version { get; set; }

        public string? FPVersion { get; set; }

        public BotDModel BotD { get; set; }

        public string? Tagging { get; set; }

        public string? F4 { get; set; }

        public string? F5 { get; set; }

        public string? Rtc { get; set; }

        public string? SessionKey { get; set; }

        public int? TaggingType { get; set; }
    }
}
