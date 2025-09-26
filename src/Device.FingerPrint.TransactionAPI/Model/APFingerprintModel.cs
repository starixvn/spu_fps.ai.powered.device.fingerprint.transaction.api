using System.Text.Json.Serialization;

namespace Device.FingerPrint.Models
{
    public class APFingerprintModel
    {
        [JsonPropertyName("au")]
        public string? Audio { get; set; }

        [JsonPropertyName("br")]
        public string? Browser { get; set; }

        [JsonPropertyName("hw")]
        public string? Hardware { get; set; }

        [JsonPropertyName("pf")]
        public string? Preferences { get; set; }

        [JsonPropertyName("gp")]
        public string? Graphic { get; set; }
    }
}
