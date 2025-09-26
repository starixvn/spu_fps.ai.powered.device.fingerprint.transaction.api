namespace Device.FingerPrint.Models
{
    public class MobileModel
    {
        public string? MachineCode { get; set; }

        public string? CustomCode { get; set; }

        public string? MediaDRMCode { get; set; }

        public bool IsMobileAppTrans { get; set; }

        public bool IsEmulator { get; set; }

        public string? ModelName { get; set; }

        public string? Manufacturer { get; set; }

        public string? Brand { get; set; }

        public string? SdkVersion { get; set; }

        public string? OS { get; set; }

        public string? Timezone { get; set; }

        public string? SystemLanguage { get; set; }

        public string? Country { get; set; }

        public string? DeviceDetails { get; set; }
    }
}
