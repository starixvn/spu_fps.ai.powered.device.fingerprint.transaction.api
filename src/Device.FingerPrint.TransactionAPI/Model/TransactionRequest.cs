namespace Device.FingerPrint.TransactionAPI.Model
{
    public class TransactionRequest
    {
        public string? Account { get; set; }

        public string? Action { get; set; }

        public string? ActionResult { get; set; }

        public string? AcceptsTypes { get; set; }

        public string? Platform { get; set; }

        public string? UserAgent { get; set; }

        public string? Screen { get; set; }

        public string? Ip { get; set; }

        public string? FakeIp { get; set; }

        public string? Url { get; set; }

        public string? Di { get; set; }

        public string? __di { get; set; }

        public string? Bi { get; set; }

        public string? Token { get; set; }

        public string? DetecasAnalysis { get; set; }

        public string? Subscriber { get; set; }

        public int SubscriberId { get; set; }

        public DateTime? LoginTime { get; set; }

        public string? CaptchaResult { get; set; }

        public string? SessionKey { get; set; }

        public DateTime TransDate { get; set; }

        public string? GroupA { get; set; }

        public string? GroupB { get; set; }

        public string? GroupC { get; set; }

        public string? GroupD { get; set; }

        public string? GroupE { get; set; }

        public string? HashFP4 { get; set; }

        public string? HashFP5 { get; set; }
    }
}
