using Newtonsoft.Json;

namespace Device.FingerPrint.Model
{
    public class DeviceInfo
    {
        [JsonProperty("na")]
        public string NA { get; set; }

        [JsonProperty("deviceCode")]
        public string DeviceCode { get; set; }

        [JsonProperty("appVersion")]
        public string AppVersion { get; set; }

        [JsonProperty("timeZone")]
        public string TimeZone { get; set; }

        [JsonProperty("userAgent")]
        public string UserAgent { get; set; }

        [JsonProperty("screen")]
        public DeviceScreen Screen { get; set; }

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("href")]
        public string HRef { get; set; }

        [JsonProperty("capturedDate")]
        public long? CapturedDate { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        public BotD BotD { get; set; }

        [JsonProperty("tag")]
        public string? Tagging { get; set; }

        [JsonProperty("f4")]
        public string? F4 { get; set; }

        [JsonProperty("f5")]
        public string? F5 { get; set; }

        [JsonProperty("rtc")]
        public string? Rtc { get; set; }

        [JsonProperty("sk")]
        public string? SessionKey { get; set; }

        [JsonProperty("sv")]
        public string? ScriptVersion { get; set; }

        [JsonProperty("v")]
        public string? FPVersion { get; set; }
    }
}
