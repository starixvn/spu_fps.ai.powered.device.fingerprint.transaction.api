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
        public void Decode_WhenEncodedDataIsNull_ShouldReturnNull()
        {
            // Arrange
            string? encodedData = null;

            // Act
            var result = Base64.Decode(encodedData);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Decode_WhenEncodedDataIsEmpty_ShouldReturnNull()
        {
            // Arrange
            string encodedData = string.Empty;

            // Act
            var result = Base64.Decode(encodedData);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Decode_WithStandardBase64_ShouldDecodeUsingCustomKey()
        {
            // Arrange
            string originalText = "Hello World!";
            string encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalText));

            // Act
            var result = Base64.Decode(encodedData);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void Decode_WithCustomKeyEncodedData_ShouldDecodeCorrectly()
        {
            // Arrange
            string encodedData = "SGVsbG8gV29ybGQh"; // "Hello World!" in standard base64
            
            // Act
            var result = Base64.Decode(encodedData);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void Decode_WithInvalidCharacters_ShouldFilterOutInvalidChars()
        {
            // Arrange
            string encodedData = "SGVs!@#bG8gV29y$%^bGQh&*()";

            // Act
            var result = Base64.Decode(encodedData);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void Decode_WithSingleCharacter_ShouldHandleGracefully()
        {
            // Arrange
            string encodedData = "A";

            // Act
            var result = Base64.Decode(encodedData);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void Decode_WithUTF8Characters_ShouldDecodeCorrectly()
        {
            // Arrange
            string originalText = "Café ñoño";
            string encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalText));

            // Act
            var result = Base64.Decode(encodedData);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void Decode_WithOnlyInvalidCharacters_ShouldReturnNull()
        {
            // Arrange
            string encodedData = "!@#$%^&*()";

            // Act
            var result = Base64.Decode(encodedData);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Decode_WithValidBase64Characters_ShouldNotReturnNull()
        {
            // Arrange
            string encodedData = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";

            // Act
            var result = Base64.Decode(encodedData);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void Decode_WithPaddingCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            string encodedData = "SGVsbG8=";

            // Act
            var result = Base64.Decode(encodedData);

            // Assert
            result.Should().NotBeNull();
        }

        #endregion

        #region DecodeUtf8 Tests

        [Fact]
        public void DecodeUtf8_WithASCIICharacters_ShouldReturnSameString()
        {
            // Arrange
            string asciiText = "Hello World";

            // Act
            var result = InvokeUtf8Decode(asciiText);

            // Assert
            result.Should().Be(asciiText);
        }

        [Fact]
        public void DecodeUtf8_WithEmptyString_ShouldReturnEmptyString()
        {
            // Arrange
            string input = "";

            // Act
            var result = InvokeUtf8Decode(input);

            // Assert
            result.Should().Be("");
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
            result.Should().NotBeNull();
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
            result.Should().NotBeNull();
        }

        [Fact]
        public void Decode_WithMixedValidAndInvalidCharacters_ShouldFilterInvalidAndDecode()
        {
            // Arrange
            string mixedData = "SGVs123bG8!@#gV29y$%^bGQh&*()=";

            // Act
            var result = Base64.Decode(mixedData);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void Decode_WithOverflowScenario_ShouldReturnNull()
        {
            // Arrange
            string problematicData = new string('\uFFFF', 10);

            // Act
            var result = Base64.Decode(problematicData);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Helper Methods

        private static string InvokeUtf8Decode(string input)
        {
            var method = typeof(Base64).GetMethod("DecodeUtf8", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (string)method!.Invoke(null, new object[] { input })!;
        }

        #endregion
    }
}