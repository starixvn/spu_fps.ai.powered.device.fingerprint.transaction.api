using System.Text.Json.Serialization;

namespace Device.FingerPrint.Models
{
    public class FingerprintRequest
    {
        [JsonPropertyName("sk")]
        public required string SessionKey { get; set; }

        [JsonPropertyName("fp")]
        public required APFingerprintModel Fingerprint { get; set; }

        [JsonPropertyName("hs")]
        public required string Hashed { get; set; }
    }
}
