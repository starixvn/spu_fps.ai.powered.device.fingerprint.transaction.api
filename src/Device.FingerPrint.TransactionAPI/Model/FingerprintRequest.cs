namespace Device.FingerPrint.TransactionAPI.Model
{
    public class FingerprintRequest
    {
        public required string SessionKey { get; set; }

        public required string Hashed { get; set; }

        public required APFingerprintModel Fingerprint { get; set; }

    }
}
