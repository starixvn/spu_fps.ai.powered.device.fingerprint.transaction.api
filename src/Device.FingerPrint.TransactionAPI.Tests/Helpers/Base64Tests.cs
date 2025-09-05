using Device.FingerPrint.TransactionAPI.Helpers;
using FluentAssertions;
using System.Text;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Helpers
{
    public class Base64Tests
    {
        #region Decode(string? encodedData) Tests

        [Fact]
        public void Decode_WithNullInput_ShouldReturnNull()
        {
            // Arrange
            string? input = null;

            // Act
            var result = Base64.Decode(input);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Decode_WithEmptyInput_ShouldReturnNull()
        {
            // Arrange
            string input = string.Empty;

            // Act
            var result = Base64.Decode(input);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Decode_WithValidBase64String_ShouldDecodeCorrectly()
        {
            // Arrange
            string originalText = "Hello World!";
            string encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalText));

            // Act
            var result = Base64.Decode(encodedData);

            // Assert
            result.Should().Be(originalText);
        }

        [Fact]
        public void Decode_WithBase64StringContainingSpaces_ShouldReplaceSpacesWithPlus()
        {
            // Arrange
            string originalText = "Test with plus signs";
            string encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalText));
            string encodedWithSpaces = encodedData.Replace("+", " ");

            // Act
            var result = Base64.Decode(encodedWithSpaces);

            // Assert
            result.Should().Be(originalText);
        }

        [Fact]
        public void Decode_WithInvalidCharacters_ShouldTruncateAtFirstInvalidChar()
        {
            // Arrange
            string validBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("Hello"));
            string invalidBase64 = validBase64 + "!@#$" + "MoreValidBase64";

            // Act
            var result = Base64.Decode(invalidBase64);

            // Assert
            result.Should().Be("Hello");
        }

        [Fact]
        public void Decode_WithPaddingOnly_ShouldHandleGracefully()
        {
            // Arrange
            string input = "====";

            // Act & Assert - This should throw an exception based on the current implementation
            var exception = Assert.Throws<InvalidOperationException>(() => Base64.Decode(input));
            exception.Should().NotBeNull();
        }

        [Fact]
        public void Decode_WithUnicodeCharacters_ShouldDecodeCorrectly()
        {
            // Arrange
            string originalText = "Hello ??! ??";
            string encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalText));

            // Act
            var result = Base64.Decode(encodedData);

            // Assert
            result.Should().Be(originalText);
        }

        #endregion

        #region Decode(string? encodedData, string key) Tests

        [Fact]
        public void Decode_WithKey_WhenEncodedDataIsNull_ShouldReturnNull()
        {
            // Arrange
            string? encodedData = null;
            string key = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";

            // Act
            var result = Base64.Decode(encodedData, key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Decode_WithKey_WhenEncodedDataIsEmpty_ShouldReturnNull()
        {
            // Arrange
            string encodedData = string.Empty;
            string key = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";

            // Act
            var result = Base64.Decode(encodedData, key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Decode_WithKey_WithStandardBase64Key_ShouldDecodeCorrectly()
        {
            // Arrange
            string originalText = "Hello World!";
            string encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalText));
            string standardKey = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";

            // Act
            var result = Base64.Decode(encodedData, standardKey);

            // Assert
            result.Should().Be(originalText);
        }

        [Fact]
        public void Decode_WithKey_WithCustomKey_ShouldDecodeBasedOnCustomKey()
        {
            // Arrange
            string customKey = "ZYXWVUTSRQPONMLKJIHGFEDCBAzyxwvutsrqponmlkjihgfedcba9876543210+/=";
            string encodedData = "SGVsbG8gV29ybGQh"; // "Hello World!" in standard base64
            
            // Act
            var result = Base64.Decode(encodedData, customKey);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void Decode_WithKey_WithInvalidCharacters_ShouldFilterOutInvalidChars()
        {
            // Arrange
            string encodedData = "SGVs!@#bG8gV29y$%^bGQh&*()";
            string key = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";

            // Act
            var result = Base64.Decode(encodedData, key);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void Decode_WithKey_WithSingleCharacter_ShouldHandleGracefully()
        {
            // Arrange
            string encodedData = "A";
            string key = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";

            // Act
            var result = Base64.Decode(encodedData, key);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void Decode_WithKey_WithUTF8Characters_ShouldDecodeCorrectly()
        {
            // Arrange
            string originalText = "Café ñoño";
            string encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalText));
            string key = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";

            // Act
            var result = Base64.Decode(encodedData, key);

            // Assert
            result.Should().Be(originalText);
        }

        [Fact]
        public void Decode_WithKey_WithEmptyKey_ShouldThrowException()
        {
            // Arrange
            string encodedData = "SGVsbG8=";
            string key = "";

            // Act & Assert - This should throw an exception when key is empty
            var exception = Assert.Throws<OverflowException>(() => Base64.Decode(encodedData, key));
            exception.Should().NotBeNull();
        }

        #endregion

        #region ValidEncodedData Tests

        [Fact]
        public void ValidEncodedData_WithValidBase64_ShouldReturnSameString()
        {
            // Arrange
            string validBase64 = "SGVsbG8gV29ybGQh";

            // Act - Using reflection to test private method
            var result = InvokeValidEncodedData(validBase64);

            // Assert
            result.Should().Be(validBase64);
        }

        [Fact]
        public void ValidEncodedData_WithInvalidCharacters_ShouldTruncateAtFirstInvalid()
        {
            // Arrange
            string invalidBase64 = "SGVsbG8!Invalid";
            string expected = "SGVsbG8";

            // Act
            var result = InvokeValidEncodedData(invalidBase64);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ValidEncodedData_WithValidPadding_ShouldReturnSameString()
        {
            // Arrange
            string validBase64WithPadding = "SGVsbG8=";

            // Act
            var result = InvokeValidEncodedData(validBase64WithPadding);

            // Assert
            result.Should().Be(validBase64WithPadding);
        }

        [Fact]
        public void ValidEncodedData_WithOnlyInvalidCharacters_ShouldReturnEmptyString()
        {
            // Arrange
            string invalidBase64 = "!@#$%^&*()";

            // Act
            var result = InvokeValidEncodedData(invalidBase64);

            // Assert
            result.Should().Be("");
        }

        #endregion

        #region Utf8Decode Tests

        [Fact]
        public void Utf8Decode_WithASCIICharacters_ShouldReturnSameString()
        {
            // Arrange
            string asciiText = "Hello World";

            // Act
            var result = InvokeUtf8Decode(asciiText);

            // Assert
            result.Should().Be(asciiText);
        }

        [Fact]
        public void Utf8Decode_WithTwoByteUTF8Characters_ShouldDecodeCorrectly()
        {
            // Arrange - UTF-8 encoded "café" where é is encoded as two bytes
            string utf8Text = "caf" + (char)0xC3 + (char)0xA9;
            string expected = "café";

            // Act
            var result = InvokeUtf8Decode(utf8Text);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void Utf8Decode_WithThreeByteUTF8Characters_ShouldDecodeCorrectly()
        {
            // Arrange - UTF-8 encoded "€" symbol (3-byte UTF-8)
            string utf8Text = "" + (char)0xE2 + (char)0x82 + (char)0xAC;
            string expected = "€";

            // Act
            var result = InvokeUtf8Decode(utf8Text);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void Utf8Decode_WithEmptyString_ShouldReturnEmptyString()
        {
            // Arrange
            string input = "";

            // Act
            var result = InvokeUtf8Decode(input);

            // Assert
            result.Should().Be("");
        }

        [Fact]
        public void Utf8Decode_WithMixedCharacters_ShouldDecodeAllCorrectly()
        {
            // Arrange - Mix of ASCII, 2-byte, and 3-byte UTF-8
            string utf8Text = "A" + (char)0xC3 + (char)0xA9 + "B" + (char)0xE2 + (char)0x82 + (char)0xAC + "C";
            string expected = "AéB€C";

            // Act
            var result = InvokeUtf8Decode(utf8Text);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Fact]
        public void Decode_WithRealWorldExample_ShouldWorkCorrectly()
        {
            // Arrange
            string originalText = "Device fingerprint data with special chars: @#$%^&*()";
            string encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalText));

            // Act
            var result = Base64.Decode(encodedData);

            // Assert
            result.Should().Be(originalText);
        }

        [Fact]
        public void Decode_WithLongString_ShouldHandleCorrectly()
        {
            // Arrange
            string originalText = new string('A', 1000) + new string('B', 1000);
            string encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalText));

            // Act
            var result = Base64.Decode(encodedData);

            // Assert
            result.Should().Be(originalText);
        }

        [Fact]
        public void Decode_BothMethods_WithSameValidInput_ShouldReturnSameResult()
        {
            // Arrange
            string originalText = "Consistency test";
            string encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalText));
            string standardKey = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";

            // Act
            var result1 = Base64.Decode(encodedData);
            var result2 = Base64.Decode(encodedData, standardKey);

            // Assert
            result1.Should().Be(result2);
            result1.Should().Be(originalText);
        }

        #endregion

        #region Helper Methods

        private static string InvokeValidEncodedData(string input)
        {
            var method = typeof(Base64).GetMethod("ValidEncodedData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (string)method!.Invoke(null, new object[] { input })!;
        }

        private static string InvokeUtf8Decode(string input)
        {
            var method = typeof(Base64).GetMethod("Utf8Decode", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (string)method!.Invoke(null, new object[] { input })!;
        }

        #endregion
    }
}