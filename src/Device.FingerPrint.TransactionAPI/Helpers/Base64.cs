using System.Text;
using System.Text.RegularExpressions;

namespace Device.FingerPrint.TransactionAPI.Helpers
{
    /// <summary>
    /// Provides Base64 decoding functionality with support for custom keys and UTF-8 encoding.
    /// </summary>
    public static class Base64
    {
        // Constants for magic numbers
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

        // Compiled regex patterns for better performance
        private static readonly Regex InvalidCharsRegex = new(@"[^A-Za-z0-9\+\/\=]", RegexOptions.Compiled, RegexTimeout);
        private static readonly Regex ValidBase64Pattern = new(@"^[A-Za-z0-9+/]*={0,2}$", RegexOptions.Compiled, RegexTimeout);
        private static readonly Regex Base64CharPattern = new(@"[A-Za-z0-9+/=]", RegexOptions.Compiled, RegexTimeout);

        private const string BASE64_KEY = "wFKfVDGZqRbyYCBi6IveMaQAh1ntWcP8ujT/U7r2xXs+J0L=4d3zEHSo5glN9kOpm";

        /// <summary>
        /// Decodes a Base64 encoded string using a custom key for character mapping.
        /// </summary>
        /// <param name="encodedData">The encoded string to decode.</param>
        /// <param name="key">The custom key used for character mapping.</param>
        /// <returns>The decoded string, or null if the input is null or empty.</returns>
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

        /// <summary>
        /// Decodes encoded data using a custom key mapping.
        /// </summary>
        /// <param name="encodedData">The cleaned encoded data.</param>
        /// <param name="key">The custom key for character mapping.</param>
        /// <returns>The decoded output string.</returns>
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

        /// <summary>
        /// Extracts four encoded values from the input data.
        /// </summary>
        /// <param name="encodedData">The encoded data string.</param>
        /// <param name="key">The key for character mapping.</param>
        /// <param name="index">The current index (passed by reference and updated).</param>
        /// <returns>A tuple containing the four encoded values.</returns>
        private static (int enc1, int enc2, int enc3, int enc4) ExtractEncodedValues(
            string encodedData, string key, ref int index)
        {
            int enc1 = GetKeyIndex(encodedData, key, ref index);
            int enc2 = GetKeyIndex(encodedData, key, ref index);
            int enc3 = GetKeyIndex(encodedData, key, ref index);
            int enc4 = GetKeyIndex(encodedData, key, ref index);

            return (enc1, enc2, enc3, enc4);
        }

        /// <summary>
        /// Gets the key index for a character at the current position.
        /// </summary>
        /// <param name="encodedData">The encoded data string.</param>
        /// <param name="key">The key for character mapping.</param>
        /// <param name="index">The current index (passed by reference and updated).</param>
        /// <returns>The key index, or 0 if beyond string length.</returns>
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

        /// <summary>
        /// Calculates the decoded characters from encoded values.
        /// </summary>
        /// <param name="enc1">First encoded value.</param>
        /// <param name="enc2">Second encoded value.</param>
        /// <param name="enc3">Third encoded value.</param>
        /// <param name="enc4">Fourth encoded value.</param>
        /// <returns>A tuple containing the three decoded character values.</returns>
        private static (int chr1, int chr2, int chr3) CalculateDecodedCharacters(
            int enc1, int enc2, int enc3, int enc4)
        {
            int chr1 = (enc1 << BitShift2) | (enc2 >> BitShift4);
            int chr2 = ((enc2 & BitMask15) << BitShift4) | (enc3 >> BitShift2);
            int chr3 = ((enc3 & BitMask3) << BitShift6) | enc4;

            return (chr1, chr2, chr3);
        }

        /// <summary>
        /// Safely converts an integer to a character.
        /// </summary>
        /// <param name="value">The integer value to convert.</param>
        /// <returns>The converted character.</returns>
        private static char ConvertToChar(int value)
        {
            if (value < 0 || value > char.MaxValue)
            {
                throw new OverflowException($"Value {value} is outside the valid character range.");
            }

            return Convert.ToChar(value);
        }

        /// <summary>
        /// Decodes UTF-8 encoded text.
        /// </summary>
        /// <param name="utfText">The UTF-8 encoded text to decode.</param>
        /// <returns>The decoded string.</returns>
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

        /// <summary>
        /// Checks if a character is an ASCII character.
        /// </summary>
        /// <param name="character">The character to check.</param>
        /// <returns>True if the character is ASCII, false otherwise.</returns>
        private static bool IsAsciiCharacter(int character) => character < AsciiThreshold;

        /// <summary>
        /// Checks if a character is a two-byte UTF-8 character.
        /// </summary>
        /// <param name="character">The character to check.</param>
        /// <returns>True if the character is a two-byte UTF-8 character, false otherwise.</returns>
        private static bool IsTwoByteUtf8Character(int character) => 
            character > TwoByteUtf8Lower && character < TwoByteUtf8Upper;

        /// <summary>
        /// Processes a two-byte UTF-8 character.
        /// </summary>
        /// <param name="utfText">The UTF-8 text being processed.</param>
        /// <param name="result">The result StringBuilder.</param>
        /// <param name="index">The current index.</param>
        /// <returns>The updated index after processing.</returns>
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

        /// <summary>
        /// Processes a three-byte UTF-8 character.
        /// </summary>
        /// <param name="utfText">The UTF-8 text being processed.</param>
        /// <param name="result">The result StringBuilder.</param>
        /// <param name="index">The current index.</param>
        /// <returns>The updated index after processing.</returns>
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

        /// <summary>
        /// Validates and cleans the encoded data by removing invalid characters.
        /// </summary>
        /// <param name="encodedData">The encoded data to validate and clean.</param>
        /// <returns>The cleaned encoded data.</returns>
        private static string ValidateAndCleanEncodedData(string encodedData)
        {
            ArgumentNullException.ThrowIfNull(encodedData);

            if (!ValidBase64Pattern.IsMatch(encodedData))
            {
                return TruncateAtFirstInvalidCharacter(encodedData);
            }

            return encodedData;
        }

        /// <summary>
        /// Truncates the string at the first invalid Base64 character.
        /// </summary>
        /// <param name="encodedData">The encoded data to truncate.</param>
        /// <returns>The truncated string containing only valid characters.</returns>
        private static string TruncateAtFirstInvalidCharacter(string encodedData)
        {
            for (int i = 0; i < encodedData.Length; i++)
            {
                if (!Base64CharPattern.IsMatch(encodedData[i].ToString()))
                {
                    return encodedData[..i];
                }
            }

            return encodedData;
        }
    }
}
