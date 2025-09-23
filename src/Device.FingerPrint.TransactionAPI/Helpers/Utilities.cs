namespace Device.FingerPrint.TransactionAPI.Helpers
{
    public static class Utilities
    {
        public static bool IsValidJson(string data)
        {
            data = data.Trim();
            return (data.StartsWith('{') && data.EndsWith('}')) ||
                   (data.StartsWith('[') && data.EndsWith(']'));
        }
    }
}
