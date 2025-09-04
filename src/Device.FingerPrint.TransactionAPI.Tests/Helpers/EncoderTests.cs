using Device.FingerPrint.TransactionAPI.Helpers;
using FluentAssertions;
using System.Text;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Helpers
{
    public class EncoderTests
    {
        [Theory]
        [InlineData("Hello World", "SGVsbG8gV29ybGQ=")]
        [InlineData("Test String", "VGVzdCBTdHJpbmc=")]
        [InlineData("", "")]
        [InlineData("A", "QQ==")]
        [InlineData("Special chars: !@#$%^&*()", "U3BlY2lhbCBjaGFyczogIUAjJCVeJiooKQ==")]
        public void Base64Encode_WithValidInput_ShouldReturnCorrectBase64String(string input, string expected)
        {
            // Act
            var result = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Encode(input);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void Base64Encode_WithUnicodeInput_ShouldEncodeCorrectly()
        {
            // Arrange
            var input = "Unicode: ????";
            var expectedBytes = Encoding.UTF8.GetBytes(input);
            var expected = Convert.ToBase64String(expectedBytes);

            // Act
            var result = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Encode(input);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void Base64Encode_WithNullInput_ShouldReturnNull()
        {
            // Act
            var result = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Encode(null);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Base64Encode_WithEmptyString_ShouldReturnEmptyString()
        {
            // Act
            var result = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Encode("");

            // Assert
            result.Should().Be("");
        }

        [Theory]
        [InlineData("SGVsbG8gV29ybGQ=", "Hello World")]
        [InlineData("VGVzdCBTdHJpbmc=", "Test String")]
        [InlineData("", "")]
        [InlineData("QQ==", "A")]
        [InlineData("U3BlY2lhbCBjaGFyczogIUAjJCVeJiooKQ==", "Special chars: !@#$%^&*()")]
        public void Base64Decode_WithValidInput_ShouldReturnCorrectDecodedString(string input, string expected)
        {
            // Act
            var result = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Decode(input);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void Base64Decode_WithUnicodeBase64_ShouldDecodeCorrectly()
        {
            // Arrange
            var originalText = "Unicode: ????";
            var base64Input = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalText));

            // Act
            var result = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Decode(base64Input);

            // Assert
            result.Should().Be(originalText);
        }

        [Fact]
        public void Base64Decode_WithNullInput_ShouldReturnNull()
        {
            // Act
            var result = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Decode(null);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Base64Decode_WithEmptyString_ShouldReturnEmptyString()
        {
            // Act
            var result = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Decode("");

            // Assert
            result.Should().Be("");
        }

        [Theory]
        [InlineData("InvalidBase64!@#")]
        [InlineData("NotBase64")]
        [InlineData("SGVsbG8=!")]
        public void Base64Decode_WithInvalidBase64Input_ShouldThrowFormatException(string invalidInput)
        {
            // Act & Assert
            var action = () => Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Decode(invalidInput);
            action.Should().Throw<FormatException>();
        }

        [Fact]
        public void Base64Encode_ThenDecode_ShouldReturnOriginalString()
        {
            // Arrange
            var originalText = "This is a round-trip test with special characters: едц!@#$%^&*()";

            // Act
            var encoded = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Encode(originalText);
            var decoded = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Decode(encoded);

            // Assert
            decoded.Should().Be(originalText);
        }

        [Fact]
        public void Base64Encode_ThenDecode_WithUnicode_ShouldReturnOriginalString()
        {
            // Arrange
            var originalText = "Unicode: ???? Emoji: ????";

            // Act
            var encoded = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Encode(originalText);
            var decoded = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Decode(encoded);

            // Assert
            decoded.Should().Be(originalText);
        }

        [Fact]
        public void Base64Decode_ThenEncode_ShouldReturnOriginalBase64String()
        {
            // Arrange
            var originalBase64 = "VGhpcyBpcyBhIHJvdW5kLXRyaXAgdGVzdA==";

            // Act
            var decoded = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Decode(originalBase64);
            var encoded = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Encode(decoded);

            // Assert
            encoded.Should().Be(originalBase64);
        }

        [Theory]
        [InlineData("Multi\nline\ntext")]
        [InlineData("Text with\ttabs")]
        [InlineData("Text with\r\ncarriage returns")]
        public void Base64Encode_WithWhitespaceCharacters_ShouldHandleCorrectly(string input)
        {
            // Act
            var encoded = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Encode(input);
            var decoded = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Decode(encoded);

            // Assert
            encoded.Should().NotBeNull();
            decoded.Should().Be(input);
        }

        [Fact]
        public void Base64Encode_WithLongString_ShouldHandleCorrectly()
        {
            // Arrange
            var longString = new string('A', 10000);

            // Act
            var encoded = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Encode(longString);
            var decoded = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Decode(encoded);

            // Assert
            encoded.Should().NotBeNull();
            decoded.Should().Be(longString);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void Base64Encode_WithWhitespaceOnlyStrings_ShouldEncodeCorrectly(string whitespaceString)
        {
            // Act
            var encoded = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Encode(whitespaceString);
            var decoded = Device.FingerPrint.TransactionAPI.Helpers.Encoder.Base64Decode(encoded);

            // Assert
            encoded.Should().NotBeNull();
            decoded.Should().Be(whitespaceString);
        }
    }
}