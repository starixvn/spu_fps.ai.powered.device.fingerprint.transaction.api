namespace Device.FingerPrint.TransactionAPI.Model
{
    public class MobileTransactionModel
    {
        public string? Account { get; set; }

        public string? Subscriber { get; set; }

        public DateTime? LoginTime { get; set; }

        public string? Action { get; set; }

        public string? ActionResult { get; set; }

        public string? AndroidCode { get; set; }

        public string? MachineCode { get; set; }

        public string? CustomCode { get; set; }

        public string? MediaDRMCode { get; set; }

        public bool IsEmulator { get; set; }

        public string? ModelName { get; set; }

        public string? Manufacturer { get; set; }

        public string? Brand { get; set; }

        public string? SdkVersion { get; set; }

        public string? OS { get; set; }

        public string? Timezone { get; set; }

        public string? SystemLanguage { get; set; }

        public string? Country { get; set; }

        public string? IP { get; set; }

        public DateTime TransDate { get; set; }
    }
}
