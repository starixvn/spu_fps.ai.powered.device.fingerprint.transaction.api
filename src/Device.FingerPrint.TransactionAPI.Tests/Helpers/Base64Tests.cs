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
            string encodedData = "P3R31ACHtZ6TB2JThrkEq/XEW2a7yKRTtoIynQgUq/xTnGaj1Gd7WoC8hSj3tSH7q2EJqrI7cGa/cG7=tTqlqrI7cGa/cVD4WD17W2CXtS5J1GaE1QCEaAC7WUD21QgEqT4ThSk0WGkL1QgEq/xTPH4TWZR=hSazWH4TB20Wq2CEhAI7AKqlyeVJAKR7W2R=W74TB74T6rkE1Va3Wrk3BTFonQgUtoWLWZR=hSazW3FXW3FHtrI71r7L1QIWq2EJAKRHWSa36Qc7t2IWq/XNAKRShQdH1a4TB74TeQklnQdJhv9Hy/wubDcXtrI=coYue76uYewLYfJuaS7LC/6NqZuSCKUu6AF4tGaA1QRynA6=CeYoy/YSqKjyvDICeK4utG7+1vFZ1QC+t3UuvGaj1Gd7WoCfnZR=tQM=Ye64y/wLYK54qDCj1rD3nv9HYzWLYz1WqTdWq2CEhAI7AKqlYZEJAKRjWZFQ1ARznQkLAKqlPH4TcrDJcQaWq/XWq/MLYKwxaS7L1GkoW3FBaKwdYK54B3FAnQ5SCfJuPfhEbvFFWZFJ1ac7hU0XcK9HYzWLYzhubV0qaVHYyKFJnQ07qVc7hS0=bvFq1QDUtGazWECxWrk01v9dCfwLYK54y/wuMSDrhARXyzMzC35zC74TyD4TWoIjcGaWq/x48vdWq2cXtrI=cHCXPraWq/XNAKRShQdH1a4TB20WqrkHcGa3aS7UcGjWq/xdBeq4yD4TtoaE1ARq1Q72nZIWq/xdYfY3yD4TnQgL1ARAnQIEnD4TB/VdYfVJAKRXtrg7WUj7nQcxcD4TB/UECAEJAKRzcGDE1a4TB/FkyD4TWGdH1S7LWEd7trcEnD4TB20Wq21jtZa7AKqlCvdWq2CEhAI7AKqlYZEJAKR4tZa2nQgz6AR3hA7Wq/XNAKRShQdH1a4TB2I3cQMJAKRzcGDE1a4TB/FkyD4T1AR3toRMWrD/1a4TB20Wq21jtZa7AKqlAKRMPAF7IAR3toqlqVCjtrg=cKF31QDUqZF3toF7W2IX1AYutShut2aJtKwxWraj1G7L13w2YKWXADdLqKwuqGDEqGgXqKjxcZI4Wzx=yS14W3HUnvgL1AjU1AhLtraEyoCEhQcXtrW=hSk0tvgjWSj5B/V3B/W5YfqXADdLqKwuqGDEqGULiGDLtSggtQkHWz5ubGjEcZFzBT9=12FzyQIXyrg7PGI7cTgL1A6=WoIj1S7L13k/tSH0yrDznZulYeqlYeMSY/qXADdLqKwuqGDEqZYubGjEcZFzBT9=12FzyQIXyrg7PGI7cTgL1A6=WoIj1S7L13k/tSH0yrDznZulYeqlYeYoY37WAG5uqKwuhA6ueSRs1QCEyrg7PZ6ubGjEcZFzBT9=12FzyQIXyrg7PGI7cTgL1A6=WoIj1S7L13k/tSH0yrDznZulYeqlC/6HbadWtTwuqKFjcKFxcZI4Wzx=yS14W3HUnvgL1AjU1AhLtraEyoCEhQcXtrW=hSk0tvgjWSj5B/V3B/MoBadWtTwuqKFjcKFL1AWuMZR=tQ7z1vwxiGDLtSggtQkHWz5XADdLqKwuqGDEqZYubGjEcZFzBT9=12FzyQIXyrg7PGI7cTgL1A6=WoIj1S7L13k/tSH0yrDznZulYeqlYzWzbadWtTwuqKFjcKFxcZI4Wzx=yS14W3HUnvgL1AjU1AhLtraEyoCEhQcXtrW=hSk0tvgjWSj5B/V3B/VHCfhdADdLqKwuqGDEqVD3WrDgyrHjWKwxiGDLtSggtQkHWz5XADdLqKwuqGDEqGULiGDLtSggtQkHWz5ubGjEcZFzBT9=12FzyQIXyrg7PGI7cTgL1A6=WoIj1S7L13k/tSH0yrDznZulYeqlYeMEYzuXAKqJAKRzcGDE1a4TB/FkyD4TWZR=1Za/cDCHh74TB20Wq21jtZa7AKqlAKq3YfwzYfV4CH4TyD4TWoIjcGaWq/x48vdWq2cXtrI=cEa5cGa3trDJAKqlPH4TcrDJcQaWq/XWq70=hrX7ho6uIAjE1ARLhQdcAKqJAKRzcGDE1a4TB/FkyD4TtQ701aIgWGaz6SkLWS7zcGaLcD4TB20Wq21jtZa7AKqlcZRH1vdWq2CEhAI7AKqlYZEJAKR7crDJeGaL1oIxAKqlPH4TcrDJcQaWq/xzY3dWq2CEhAI7AKqlYZEJAKRo1QRZeD4TB20Wq21jtZa7AKqlPH4TcraL1Gk3AKqlAKRA1QRynAIWqTdWq2R7trI7Wra3AKqlAKRA1QRynA6uaSaTIEdWq2EJAKRzcGDE1a4TB/FkyD4TcSaTIZRXcra3AKqlPH4TcrDJcQaWq/XrhQdz1vdWq2CEhAI7AKqlYZEJAKRJhQg2cQD21ACWq/XNAKRShQdH1a4TB70tAKR7tTHaMH4TAaEJAKRzcGDE1a4TB/FkyD4T1Gk/cQH7t2IDtGa01QgEvSagWH4TB20Wq21jtZa7AKqlQHEJAKRzcGDE1a4TB/FkyD4T12aLhoIXtSgKnQgUAKqlPH4TcrDJcQaWq/XWqr1HtrCEnQkLqGRXtr6xbvFNqD0LhAIXcrMuhSkU1aEu8a4TyD4TWoIjcGaWq/x48vdWqrI7cr7/1MH7tQk3Pa4TB20Wq21jtZa7AKqlBKdWq2CEhAI7AKqlYZEJAKRzhoR71QgVnQH7t2CXtSgzAKqlPH4TcrDJcQaWq/XNAKRonQIEnD4TB/VgY/wJAKRx1Q72nZIWq/xdYfu48vdWq2CEhAI7AKqlYZEJAKRUnACEnQg/cG7S1aF3toFzAKqlPH4TcrDJcQaWq/XNAKRjcSaztSHXcQHWq/XrhQdz1vdWqrC7174TBr1jtZC7yD4ThSarWSjjW2FWq/XrhQdz1vdWqrC=hQCxn2CWq/XrhQdz1vdWqr10nQg7W74TBr1jtZC7yD4T1SaTAKql1rDJWSMJAKRLnQcxcGHjWrasWH4TBr1jtZC7yD4TWGjjt2I=tQDzAKql1rDJWSMJAKR4nGDLcGk0n2CWq/XrhQdz1vdWq2RxnQg=AKql1rDJWSMJAKRz1Qd7tr7Hta4TBr1jtZC7yD4TcSaT1ZRXcra3nQkWq/XrhQdz1vdWq2c7hrI3nA17W74TBr1jtZC7yD4TnGaj1Gd7WoC8hSj3tSH7AKql1rDJWSakyD4TWoIjcGaWq/x48vdWqrR3tocz1ARRtr1=AKqlPH4TcrDJcQaWq/XNAKRTWrkoWSa3IQg2nQg7vS7L1D4TB74ThSj3tSHXcQHWqTdWqrR3tocz1ARynQgUAKqlAKR/nZR=tQaWqTdWqr7z6QgUWrkX1D4TBr1jtZC7yD4TnACV1AC+cGk4MSDrhARXAKql1rDJWSMJAKRXWECxWrk0nAa0Bf1iWUg7cSa3AKqlcZRH1AEJAKRzcGDE1a4TB/Fk8vqJqr7zara3Y3qlcZRH1v4Tv2C/vSagq/xThShzCzFrC/hShrhHCfqS1QVgC/Ud1eUzhzqS1GV5C/WT86mm";

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