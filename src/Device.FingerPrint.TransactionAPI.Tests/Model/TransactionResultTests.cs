using Device.FingerPrint.Models;
using FluentAssertions;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Model
{
    /// <summary>
    /// Tests for TransactionResult constants and message mapping
    /// </summary>
    public class TransactionResultTests
    {
        #region Constants Tests

        [Fact]
        public void Success_ShouldHaveCorrectValue()
        {
            // Assert
            TransactionResult.Success.Should().Be("000");
        }

        [Fact]
        public void Allow_ShouldHaveCorrectValue()
        {
            // Assert
            TransactionResult.Allow.Should().Be("001");
        }

        [Fact]
        public void Review_ShouldHaveCorrectValue()
        {
            // Assert
            TransactionResult.Review.Should().Be("002");
        }

        [Fact]
        public void Deny_ShouldHaveCorrectValue()
        {
            // Assert
            TransactionResult.Deny.Should().Be("003");
        }

        [Fact]
        public void EmptyAccount_ShouldHaveCorrectValue()
        {
            // Assert
            TransactionResult.EmptyAccount.Should().Be("006");
        }

        [Fact]
        public void EmptyDevice_ShouldHaveCorrectValue()
        {
            // Assert
            TransactionResult.EmptyDevice.Should().Be("007");
        }

        [Fact]
        public void NotExistAction_ShouldHaveCorrectValue()
        {
            // Assert
            TransactionResult.NotExistAction.Should().Be("008");
        }

        [Fact]
        public void NotExistSubscriber_ShouldHaveCorrectValue()
        {
            // Assert
            TransactionResult.NotExistSubscriber.Should().Be("009");
        }

        [Fact]
        public void UnderMaintenance_ShouldHaveCorrectValue()
        {
            // Assert
            TransactionResult.UnderMaintenance.Should().Be("010");
        }

        [Fact]
        public void Others_ShouldHaveCorrectValue()
        {
            // Assert
            TransactionResult.Others.Should().Be("1000");
        }

        #endregion

        #region GetMessage Tests

        [Theory]
        [InlineData("000", "Success")]
        [InlineData("001", "Allow")]
        [InlineData("002", "Review")]
        [InlineData("003", "Deny")]
        [InlineData("006", "Account is empty")]
        [InlineData("007", "Device is empty")]
        [InlineData("008", "Action doesn't exist")]
        [InlineData("009", "Subscriber is invalid")]
        [InlineData("010", "Under Maintenance")]
        [InlineData("1000", "Other error")]
        public void GetMessage_WithValidCodes_ShouldReturnCorrectMessage(string code, string expectedMessage)
        {
            // Act
            var result = TransactionResult.GetMessage(code);

            // Assert
            result.Should().Be(expectedMessage);
        }

        [Theory]
        [InlineData("999")]
        [InlineData("004")]
        [InlineData("005")]
        [InlineData("")]
        [InlineData("invalid")]
        [InlineData("ABC")]
        public void GetMessage_WithInvalidCodes_ShouldReturnUnknownErrorMessage(string invalidCode)
        {
            // Act
            var result = TransactionResult.GetMessage(invalidCode);

            // Assert
            result.Should().Be("Unknown error code");
        }

        [Fact]
        public void GetMessage_WithNullCode_ShouldReturnUnknownErrorMessage()
        {
            // Act
            var result = TransactionResult.GetMessage(null!);

            // Assert
            result.Should().Be("Unknown error code");
        }

        [Theory]
        [InlineData("000")]
        [InlineData("001")]
        [InlineData("002")]
        [InlineData("003")]
        public void GetMessage_WithSuccessAndDecisionCodes_ShouldReturnAppropriateMessages(string code)
        {
            // Act
            var result = TransactionResult.GetMessage(code);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().NotBe("Unknown error code");
        }

        [Theory]
        [InlineData("006")]
        [InlineData("007")]
        [InlineData("008")]
        [InlineData("009")]
        [InlineData("010")]
        [InlineData("1000")]
        public void GetMessage_WithErrorCodes_ShouldReturnErrorMessages(string errorCode)
        {
            // Act
            var result = TransactionResult.GetMessage(errorCode);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().NotBe("Unknown error code");
            result.Should().NotBe("Success");
            result.Should().NotBe("Allow");
        }

        #endregion

        #region Case Sensitivity Tests

        [Theory]
        [InlineData("000")]
        [InlineData("001")]
        [InlineData("010")]
        public void GetMessage_WithLowerCaseCodes_ShouldBeCaseSensitive(string code)
        {
            // Arrange
            var upperCode = code.ToUpperInvariant();
            var lowerCode = code.ToLowerInvariant();

            // Act
            var upperResult = TransactionResult.GetMessage(upperCode);
            var lowerResult = TransactionResult.GetMessage(lowerCode);

            // Assert
            // These should be the same since our codes are numeric
            upperResult.Should().Be(lowerResult);
        }

        #endregion

        #region Edge Cases Tests

        [Theory]
        [InlineData(" 000 ")]
        [InlineData("000 ")]
        [InlineData(" 000")]
        public void GetMessage_WithWhitespaceAroundCodes_ShouldReturnUnknownError(string codeWithWhitespace)
        {
            // Act
            var result = TransactionResult.GetMessage(codeWithWhitespace);

            // Assert
            result.Should().Be("Unknown error code");
        }

        [Theory]
        [InlineData("0000")]
        [InlineData("00")]
        [InlineData("0")]
        public void GetMessage_WithDifferentLengthCodes_ShouldHandleCorrectly(string code)
        {
            // Act
            var result = TransactionResult.GetMessage(code);

            // Assert
            if (code == "0")
            {
                result.Should().Be("Unknown error code");
            }
            else if (code == "00")
            {
                result.Should().Be("Unknown error code");
            }
            else if (code == "0000")
            {
                result.Should().Be("Unknown error code");
            }
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void GetMessage_CalledMultipleTimes_ShouldPerformConsistently()
        {
            // Arrange
            const int iterations = 10000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                TransactionResult.GetMessage("000");
                TransactionResult.GetMessage("001");
                TransactionResult.GetMessage("999"); // Invalid code
            }

            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete in less than 1 second
        }

        [Fact]
        public void GetMessage_WithAllValidCodes_ShouldReturnUniqueMessages()
        {
            // Arrange
            var validCodes = new[] { "000", "001", "002", "003", "006", "007", "008", "009", "010", "1000" };
            var messages = new List<string>();

            // Act
            foreach (var code in validCodes)
            {
                messages.Add(TransactionResult.GetMessage(code));
            }

            // Assert
            messages.Should().OnlyHaveUniqueItems();
            messages.Should().NotContain("Unknown error code");
        }

        #endregion

        #region Message Content Tests

        [Fact]
        public void GetMessage_AllErrorMessages_ShouldBeDescriptive()
        {
            // Arrange
            var errorCodes = new[] { "006", "007", "008", "009", "010", "1000" };

            // Act & Assert
            foreach (var code in errorCodes)
            {
                var message = TransactionResult.GetMessage(code);
                message.Should().NotBeNullOrEmpty();
                message.Length.Should().BeGreaterThan(5); // Should be descriptive
            }
        }

        [Fact]
        public void GetMessage_SuccessCodes_ShouldHavePositiveWording()
        {
            // Arrange
            var successCodes = new[] { "000", "001" };

            // Act & Assert
            foreach (var code in successCodes)
            {
                var message = TransactionResult.GetMessage(code);
                message.Should().NotContain("error", because: "success codes should not contain 'error'");
                message.Should().NotContain("invalid", because: "success codes should not contain 'invalid'");
                message.Should().NotContain("empty", because: "success codes should not contain 'empty'");
            }
        }

        [Fact]
        public void GetMessage_ErrorCodes_ShouldHaveAppropriateWording()
        {
            // Arrange & Act & Assert
            TransactionResult.GetMessage("006").Should().Contain("empty", because: "empty account error should mention 'empty'");
            TransactionResult.GetMessage("007").Should().Contain("empty", because: "empty device error should mention 'empty'");
            TransactionResult.GetMessage("008").Should().Contain("exist", because: "non-existent action should mention 'exist'");
            TransactionResult.GetMessage("009").Should().Contain("invalid", because: "invalid subscriber should mention 'invalid'");
            TransactionResult.GetMessage("010").Should().Contain("Maintenance", because: "maintenance mode should mention 'Maintenance'");
            TransactionResult.GetMessage("1000").Should().Contain("error", because: "general error should mention 'error'");
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public async Task GetMessage_ConcurrentCalls_ShouldBeThreadSafe()
        {
            // Arrange
            const int concurrentTasks = 100;
            const int callsPerTask = 100;
            var tasks = new List<Task<Dictionary<string, string>>>();

            // Act
            for (int i = 0; i < concurrentTasks; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var results = new Dictionary<string, string>();
                    for (int j = 0; j < callsPerTask; j++)
                    {
                        var code = (j % 10).ToString("000");
                        results[code] = TransactionResult.GetMessage(code);
                    }
                    return results;
                }));
            }

            var allResults = await Task.WhenAll(tasks);

            // Assert
            allResults.Should().NotBeEmpty();
            
            // Check that all tasks completed successfully
            foreach (var results in allResults)
            {
                results.Should().NotBeEmpty();
            }

            // Verify consistency across all tasks
            var firstTaskResults = allResults[0];
            foreach (var taskResults in allResults)
            {
                foreach (var kvp in firstTaskResults)
                {
                    if (taskResults.ContainsKey(kvp.Key))
                    {
                        taskResults[kvp.Key].Should().Be(kvp.Value, 
                            because: "concurrent calls should return consistent results");
                    }
                }
            }
        }

        #endregion
    }
}