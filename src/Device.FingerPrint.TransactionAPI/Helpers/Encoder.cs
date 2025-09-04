using System.Text;

namespace Device.FingerPrint.TransactionAPI.Helpers
{
    public static class Encoder
    {
        public static string? Base64Encode(string? plainText)
        {
            if(plainText is null)
            {
                return null;
            }

            if(plainText == "")
            {
                return "";
            }

            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
        public static string? Base64Decode(string? base64EncodedData)
        {
            if (base64EncodedData is null)
            {
                return null;
            }

            if(base64EncodedData == "")
            {
                return "";
            }

            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
