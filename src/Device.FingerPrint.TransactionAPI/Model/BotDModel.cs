namespace Device.FingerPrint.Model
{
    public class BotDModel
    {
        public BotDResultModel Result { get; set; }

        public string Detection { get; set; }

        public string Component { get; set; }

        public bool IsVer3 { get; set; }

        public string JscKey { get; set; }
    }

    public class BotDResultModel
    {
        public bool Bot { get; set; }

        public string BotKind { get; set; }
    }
}
