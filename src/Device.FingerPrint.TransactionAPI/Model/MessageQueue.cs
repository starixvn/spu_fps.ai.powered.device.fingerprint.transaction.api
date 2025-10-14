namespace Device.FingerPrint.Model
{
    [Serializable]
    public class MessageQueue
    {
        public string? OcelotRequestId { get; set; }

        public Guid CorrelationID { get; set; }

        public string? LinkedBy { get; set; }

        public string? Account { get; set; }

        public string? Action { get; set; }

        public string? ActionResult { get; set; }

        public DeviceModel? Device { get; set; }

        public string? IP { get; set; }

        public string? FakeIP { get; set; }

        public string? Subscriber { get; set; }

        public int SubscriberId { get; set; }

        public DateTime TransDate { get; set; }

        public DateTimeOffset SubscriberDate { get; set; }

        public string? Token { get; set; }

        public string? Recaptcha { get; set; }

        public string? SessionKey { get; set; }

        public string? BotD { get; set; }

        public MobileModel? Mobile { get; set; }

        public APFingerprintModel? Fingerprint { get; set; }

        public DeviceInfoModel DeviceInfo { get; set; }

        public bool IsNewFlow { get; set; }
    }
}
