namespace Device.FingerPrint.TransactionAPI.Model
{
    public class WhoamiModel
    {
        public string? Environment { get; set; }

        public string? ServerName { get; set; }

        public string? LocalIP { get; set; }

        public DateTimeOffset ServerTime { get; set; }

        public string? RealIP { get; set; }

        public string? FakeIP { get; set; }

        public string? XFW { get; set; }

        public string? ClientIP { get; set; }

        public string? ActualIP { get; set; }

    }
}
