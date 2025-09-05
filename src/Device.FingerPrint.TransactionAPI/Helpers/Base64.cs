using System.Text;
using System.Text.RegularExpressions;

namespace Device.FingerPrint.TransactionAPI.Helpers
{
    public static class Base64
    {
        public static string? Decode(string? encodedData)
        {
            if(string.IsNullOrEmpty(encodedData))
            {
                return null;
            }

            var validEncodedData = ValidEncodedData(encodedData);

            byte[] bytes = Convert.FromBase64String(validEncodedData.Replace(" ", "+"));

            return Encoding.UTF8.GetString(bytes);
        }

        public static string? Decode(string? encodedData, string key)
        {
            if (string.IsNullOrEmpty(encodedData))
            {
                return null;
            }

            string output = "";
            int chr1, chr2, chr3;
            int enc1, enc2, enc3, enc4;
            int i = 0;

            encodedData = Regex.Replace(encodedData, "[^A-Za-z0-9\\+\\/\\=]", "");

            while (i < encodedData.Length)
            {
                enc1 = key.IndexOf(encodedData[i++]);
                enc2 = i < encodedData.Length ? key.IndexOf(encodedData[i++]) : 0;
                enc3 = i < encodedData.Length ? key.IndexOf(encodedData[i++]) : 0;
                enc4 = i < encodedData.Length ? key.IndexOf(encodedData[i++]) : 0;

                chr1 = (enc1 << 2) | (enc2 >> 4);
                chr2 = ((enc2 & 15) << 4) | (enc3 >> 2);
                chr3 = ((enc3 & 3) << 6) | enc4;

                output += Convert.ToChar(chr1);

                if (enc3 != 64)
                {
                    output += Convert.ToChar(chr2);
                }
                if (enc4 != 64)
                {
                    output += Convert.ToChar(chr3);
                }
            }

            output = Utf8Decode(output);

            return output;
        }

        private static string Utf8Decode(string utfText)
        {
            string str = "";
            int i = 0;
            int c, c2, c3;

            while (i < utfText.Length)
            {
                c = utfText[i];

                if (c < 128)
                {
                    str += Convert.ToChar(c);
                    i++;
                }
                else if ((c > 191) && (c < 224))
                {
                    c2 = utfText[i + 1];
                    str += Convert.ToChar(((c & 31) << 6) | (c2 & 63));
                    i += 2;
                }
                else
                {
                    c2 = utfText[i + 1];
                    c3 = utfText[i + 2];
                    str += Convert.ToChar(((c & 15) << 12) | ((c2 & 63) << 6) | (c3 & 63));
                    i += 3;
                }
            }

            return str;
        }


        private static string ValidEncodedData(string encodedData)
        {
            string pattern = "^[A-Za-z0-9+/]*={0,2}$";

            Match match = Regex.Match(encodedData, pattern);

            if (!match.Success)
            {
                int indexOfFirstInvalidChar = encodedData.IndexOf(encodedData.First(c => !Regex.IsMatch(c.ToString(), "[A-Za-z0-9+/=]")));

                encodedData = encodedData.Substring(0, indexOfFirstInvalidChar);
            }

            return encodedData;
        }
    }
}
