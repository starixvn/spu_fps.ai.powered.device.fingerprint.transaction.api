namespace Device.FingerPrint.TransactionAPI.Model
{
    public class TransactionResult
    {
        public const string Success = "000";

        public const string Allow = "001";
        public const string Review = "002";
        public const string Deny = "003";

        public const string EmptyAccount = "006";
        public const string EmptyDevice = "007";
        public const string NotExistAction = "008";
        public const string NotExistSubscriber = "009";
        public const string UnderMaintenance = "010";
        public const string Others = "1000";

        private static readonly Dictionary<string, string> MessageMap = new Dictionary<string, string>
        {
            ["000"] = "Success",
            ["001"] = "Allow",
            ["002"] = "Review",
            ["003"] = "Deny",
            ["006"] = "Account is empty",
            ["007"] = "Device is empty",
            ["008"] = "Action doesn't exist",
            ["009"] = "Subscriber is invalid",
            ["010"] = "Under Maintenance",
            ["1000"] = "Other error"
        };

        public static string GetMessage(string code)
        {
            return code != null && MessageMap.TryGetValue(code, out var message) ? message : "Unknown error code";
        }
    }
}
