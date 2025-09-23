using System.Text.Json.Serialization;

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
        [JsonPropertyName("tag")]
        public string? Tagging { get; set; }

        [JsonPropertyName("f4")]
        public string? F4 { get; set; }

        [JsonPropertyName("f5")]
        public string? F5 { get; set; }

        [JsonPropertyName("rtc")]
        public string? Rtc { get; set; }

        [JsonPropertyName("sk")]
        public string? SessionKey { get; set; }

        [JsonPropertyName("v")]
        public string? NewVersion { get; set; }
    }
}
