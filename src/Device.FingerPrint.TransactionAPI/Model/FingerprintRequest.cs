using System.Text.Json.Serialization;

namespace Device.FingerPrint.TransactionAPI.Model
{
    public class FingerprintRequest
    {
        [JsonPropertyName("sk")]
        public required string SessionKey { get; set; }

        [JsonPropertyName("hs")]
        public required string Hashed { get; set; }

        [JsonPropertyName("fp")]
        public required APFingerprintModel Fingerprint { get; set; }

    }
}
