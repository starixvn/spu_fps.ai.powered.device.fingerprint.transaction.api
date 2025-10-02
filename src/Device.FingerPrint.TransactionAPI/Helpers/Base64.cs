using System.Text;
using System.Text.RegularExpressions;

namespace Device.FingerPrint.TransactionAPI.Helpers
{
    public static class Base64
    {
        private const int Base64PaddingChar = 64;
        private const int AsciiThreshold = 128;
        private const int TwoByteUtf8Lower = 191;
        private const int TwoByteUtf8Upper = 224;
        private const int BitShift2 = 2;
        private const int BitShift4 = 4;
        private const int BitShift6 = 6;
        private const int BitShift12 = 12;
        private const int BitMask15 = 15;
        private const int BitMask31 = 31;
        private const int BitMask63 = 63;
        private const int BitMask3 = 3;

        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(1000);

        private static readonly Regex InvalidCharsRegex = new(@"[^A-Za-z0-9\+\/\=]", RegexOptions.Compiled, RegexTimeout);

        private const string BASE64_KEY = "wFKfVDGZqRbyYCBi6IveMaQAh1ntWcP8ujT/U7r2xXs+J0L=4d3zEHSo5glN9kOpm";

        public static string NormalDecode(string encodedData)
        {
            var validEncodedData = ValidEncodedData(encodedData);

            byte[] bytes = Convert.FromBase64String(validEncodedData.Replace(" ", "+"));

            return Encoding.UTF8.GetString(bytes);
        }

        public static string? Decode(string? encodedData)
        {
            if (string.IsNullOrEmpty(encodedData))
            {
                return null;
            }

            try
            {
                var cleanedData = InvalidCharsRegex.Replace(encodedData, string.Empty);
                
                // If all characters were invalid and removed, return null
                if (string.IsNullOrEmpty(cleanedData))
                {
                    return null;
                }
                
                var decodedOutput = DecodeWithCustomKey(cleanedData, BASE64_KEY);
                return DecodeUtf8(decodedOutput);
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or OverflowException)
            {
                // Return null for invalid character conversion to maintain consistency
                return null;
            }
        }

        private static string DecodeWithCustomKey(string encodedData, string key)
        {
            var output = new StringBuilder(encodedData.Length);
            int i = 0;

            while (i < encodedData.Length)
            {
                var (enc1, enc2, enc3, enc4) = ExtractEncodedValues(encodedData, key, ref i);
                var (chr1, chr2, chr3) = CalculateDecodedCharacters(enc1, enc2, enc3, enc4);

                output.Append(ConvertToChar(chr1));

                if (enc3 != Base64PaddingChar)
                {
                    output.Append(ConvertToChar(chr2));
                }

                if (enc4 != Base64PaddingChar)
                {
                    output.Append(ConvertToChar(chr3));
                }
            }

            return output.ToString();
        }

        private static (int enc1, int enc2, int enc3, int enc4) ExtractEncodedValues(
            string encodedData, string key, ref int index)
        {
            int enc1 = GetKeyIndex(encodedData, key, ref index);
            int enc2 = GetKeyIndex(encodedData, key, ref index);
            int enc3 = GetKeyIndex(encodedData, key, ref index);
            int enc4 = GetKeyIndex(encodedData, key, ref index);

            return (enc1, enc2, enc3, enc4);
        }

        private static int GetKeyIndex(string encodedData, string key, ref int index)
        {
            if (index >= encodedData.Length)
            {
                return 0;
            }

            int keyIndex = key.IndexOf(encodedData[index], StringComparison.Ordinal);
            index++;
            return keyIndex >= 0 ? keyIndex : 0;
        }

        private static (int chr1, int chr2, int chr3) CalculateDecodedCharacters(
            int enc1, int enc2, int enc3, int enc4)
        {
            int chr1 = (enc1 << BitShift2) | (enc2 >> BitShift4);
            int chr2 = ((enc2 & BitMask15) << BitShift4) | (enc3 >> BitShift2);
            int chr3 = ((enc3 & BitMask3) << BitShift6) | enc4;

            return (chr1, chr2, chr3);
        }

        private static char ConvertToChar(int value)
        {
            if (value < 0 || value > char.MaxValue)
            {
                throw new OverflowException($"Value {value} is outside the valid character range.");
            }

            return Convert.ToChar(value);
        }

        private static string DecodeUtf8(string utfText)
        {
            ArgumentNullException.ThrowIfNull(utfText);

            var result = new StringBuilder(utfText.Length);
            int i = 0;

            while (i < utfText.Length)
            {
                int currentChar = utfText[i];

                if (IsAsciiCharacter(currentChar))
                {
                    result.Append(ConvertToChar(currentChar));
                    i++;
                }
                else if (IsTwoByteUtf8Character(currentChar))
                {
                    i = ProcessTwoByteUtf8Character(utfText, result, i);
                }
                else
                {
                    i = ProcessThreeByteUtf8Character(utfText, result, i);
                }
            }

            return result.ToString();
        }

        private static bool IsAsciiCharacter(int character) => character < AsciiThreshold;

        private static bool IsTwoByteUtf8Character(int character) => 
            character > TwoByteUtf8Lower && character < TwoByteUtf8Upper;

        private static int ProcessTwoByteUtf8Character(string utfText, StringBuilder result, int index)
        {
            if (index + 1 >= utfText.Length)
            {
                return index + 1; // Skip invalid sequence
            }

            int firstByte = utfText[index];
            int secondByte = utfText[index + 1];
            int decodedChar = ((firstByte & BitMask31) << BitShift6) | (secondByte & BitMask63);
            
            result.Append(ConvertToChar(decodedChar));
            return index + 2;
        }

        private static int ProcessThreeByteUtf8Character(string utfText, StringBuilder result, int index)
        {
            if (index + 2 >= utfText.Length)
            {
                return index + 1; // Skip invalid sequence
            }

            int firstByte = utfText[index];
            int secondByte = utfText[index + 1];
            int thirdByte = utfText[index + 2];
            int decodedChar = ((firstByte & BitMask15) << BitShift12) | 
                             ((secondByte & BitMask63) << BitShift6) | 
                             (thirdByte & BitMask63);
            
            result.Append(ConvertToChar(decodedChar));
            return index + 3;
        }

        private static string ValidEncodedData(string encodedData)
        {
            const string pattern = "^[A-Za-z0-9+/]*={0,2}$";

            Match match = Regex.Match(
                            encodedData,
                            pattern,
                            RegexOptions.None,
                            TimeSpan.FromSeconds(1) 
                        );
            if (!match.Success)
            {
                int indexOfFirstInvalidChar = encodedData.IndexOf(encodedData.First(c => !Regex.IsMatch(
                                                                        c.ToString(),
                                                                        "[A-Za-z0-9+/=]",
                                                                        RegexOptions.None,
                                                                        TimeSpan.FromSeconds(1) // set timeout
                                                                    )));

                encodedData = encodedData.Substring(0, indexOfFirstInvalidChar);
            }

            return encodedData;
        }
    }
}
