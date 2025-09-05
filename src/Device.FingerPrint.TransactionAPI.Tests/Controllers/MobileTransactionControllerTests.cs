using Device.FingerPrint.TransactionAPI.Controllers;
using Device.FingerPrint.TransactionAPI.Model;
using Device.FingerPrint.TransactionAPI.Queue;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using System.Text;
using Xunit;
using System.Diagnostics;

namespace Device.FingerPrint.TransactionAPI.Tests.Controllers
{
    /// <summary>
    /// Comprehensive test suite for MobileTransactionController
    /// Tests mobile transaction processing including:
    /// - Mobile transaction insertion with queue operations
    /// - Base64 encoding/serialization functionality
    /// - HTTP header extraction for IP addresses
    /// - Mobile model transformation and mapping
    /// - Error handling and exception scenarios
    /// - Performance and concurrency testing
    /// </summary>
    public class MobileTransactionControllerTests
    {
        private readonly Mock<IAppSettings> _mockAppSettings;
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IMassTransitProducer> _mockMassTransitProducer;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpRequest> _mockHttpRequest;
        private readonly Mock<IHeaderDictionary> _mockHeaders;
        private readonly MobileTransactionController _controller;

        public MobileTransactionControllerTests()
        {
            _mockAppSettings = new Mock<IAppSettings>();
            _mockLogger = new Mock<ILogger>();
            _mockMassTransitProducer = new Mock<IMassTransitProducer>();
            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpRequest = new Mock<HttpRequest>();
            _mockHeaders = new Mock<IHeaderDictionary>();

            _controller = new MobileTransactionController(
                _mockAppSettings.Object,
                _mockLogger.Object,
                _mockMassTransitProducer.Object);

            SetupDefaultMocks();
        }

        private void SetupDefaultMocks()
        {
            // Setup default app settings
            _mockAppSettings.Setup(x => x.MobileQueueName).Returns("test-mobile-queue");

            // Setup HttpContext mock chain
            _mockHttpContext.Setup(x => x.Request).Returns(_mockHttpRequest.Object);
            _mockHttpRequest.Setup(x => x.Headers).Returns(_mockHeaders.Object);
            _mockHeaders.Setup(x => x["Fps-Forwarded-For"]).Returns("192.168.1.100");

            // Assign mocked HttpContext to controller
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            };
        }

        #region InsertTransactionAsync Tests

        [Fact]
        public void InsertTransactionAsync_WithValidTransaction_ShouldReturnOkAndPushToQueue()
        {
            // Arrange
            var mobileTransaction = CreateValidMobileTransaction();

            // Act
            var result = _controller.InsertTransactionAsync(mobileTransaction);

            // Assert
            result.Should().BeOfType<OkResult>();

            // Verify IP was set from header
            mobileTransaction.IP.Should().Be("192.168.1.100");

            // Verify queue operation was called
            _mockMassTransitProducer.Verify(x => x.Send(It.IsAny<MessageQueue>(), _mockAppSettings.Object.MobileQueueName), Times.Once);
        }

        [Fact]
        public void InsertTransactionAsync_WithNullTransaction_ShouldReturnBadRequest()
        {
            // Arrange
            MobileTransactionModel? transaction = null;

            // Act
            var result = _controller.InsertTransactionAsync(transaction!);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("Invalid transaction data.");

            // Verify no queue operation was called
            _mockMassTransitProducer.Verify(x => x.Send(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void InsertTransactionAsync_WithException_ShouldReturnInternalServerErrorAndLogException()
        {
            // Arrange
            var mobileTransaction = CreateValidMobileTransaction();
            var exception = new Exception("Queue connection failed");

            _mockMassTransitProducer.Setup(x => x.Send(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Throws(exception);

            // Act
            var result = _controller.InsertTransactionAsync(mobileTransaction);

            // Assert
            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

            // Verify error was logged
            _mockLogger.Verify(x => x.Error(exception.Message, exception), Times.Once);
        }

        [Fact]
        public void InsertTransactionAsync_WithEmptyForwardedForHeader_ShouldHandleGracefully()
        {
            // Arrange
            var mobileTransaction = CreateValidMobileTransaction();
            _mockHeaders.Setup(x => x["Fps-Forwarded-For"]).Returns(""); 

            // Act
            var result = _controller.InsertTransactionAsync(mobileTransaction);

            // Assert
            result.Should().BeOfType<OkResult>();
            mobileTransaction.IP.Should().BeEmpty();
        }

        [Fact]
        public void InsertTransactionAsync_WithMissingForwardedForHeader_ShouldHandleGracefully()
        {
            // Arrange
            var mobileTransaction = CreateValidMobileTransaction();
            _mockHeaders.Setup(x => x["Fps-Forwarded-For"]).Returns((string?)null);

            // Act
            var result = _controller.InsertTransactionAsync(mobileTransaction);

            // Assert
            result.Should().BeOfType<OkResult>();
            mobileTransaction.IP.Should().BeEmpty(); // ToString() on null StringValues returns empty string, not null
        }

        [Fact]
        public void InsertTransactionAsync_WithMultipleIPsInHeader_ShouldUseFullHeaderValue()
        {
            // Arrange
            var mobileTransaction = CreateValidMobileTransaction();
            var multipleIPs = "192.168.1.100, 10.0.0.1, 203.0.113.1";
            _mockHeaders.Setup(x => x["Fps-Forwarded-For"]).Returns(multipleIPs);

            // Act
            var result = _controller.InsertTransactionAsync(mobileTransaction);

            // Assert
            result.Should().BeOfType<OkResult>();
            mobileTransaction.IP.Should().Be(multipleIPs);
        }

        [Fact]
        public void InsertTransactionAsync_WithComplexMobileTransaction_ShouldMapAllProperties()
        {
            // Arrange
            var mobileTransaction = new MobileTransactionModel
            {
                Account = "premium-mobile-account",
                Subscriber = "mobile-subscriber",
                Action = "mobile-login",
                ActionResult = "success",
                AndroidCode = "android-specific-123",
                MachineCode = "machine-fallback-456",
                CustomCode = "custom-mobile-789",
                MediaDRMCode = "drm-code-abc",
                IsEmulator = true,
                ModelName = "Galaxy S23",
                Manufacturer = "Samsung",
                Brand = "Samsung",
                SdkVersion = "33",
                OS = "Android 13",
                Timezone = "Asia/Ho_Chi_Minh",
                SystemLanguage = "vi-VN",
                Country = "VN",
                TransDate = new DateTime(2024, 1, 15, 14, 30, 0),
                LoginTime = DateTime.Now
            };

            MessageQueue? capturedMessage = null;
            _mockMassTransitProducer.Setup(x => x.Send(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Callback<MessageQueue, string>((msg, queue) => capturedMessage = msg);

            // Act
            var result = _controller.InsertTransactionAsync(mobileTransaction);

            // Assert
            result.Should().BeOfType<OkResult>();
            capturedMessage.Should().NotBeNull();

            // Verify MessageQueue mapping
            capturedMessage!.Account.Should().Be(mobileTransaction.Account);
            capturedMessage.Action.Should().Be(mobileTransaction.Action);
            capturedMessage.ActionResult.Should().Be(mobileTransaction.ActionResult);
            capturedMessage.Subscriber.Should().Be(mobileTransaction.Subscriber);
            capturedMessage.IP.Should().Be("192.168.1.100"); // From header
            capturedMessage.TransDate.Should().Be(mobileTransaction.TransDate);
            capturedMessage.SubscriberDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));

            // Verify Mobile model mapping
            capturedMessage.Mobile.Should().NotBeNull();
            var mobile = capturedMessage.Mobile!;
            mobile.MachineCode.Should().Be(mobileTransaction.AndroidCode); // Should prefer AndroidCode
            mobile.Brand.Should().Be(mobileTransaction.Brand);
            mobile.Country.Should().Be(mobileTransaction.Country);
            mobile.CustomCode.Should().Be(mobileTransaction.CustomCode);
            mobile.IsEmulator.Should().Be(mobileTransaction.IsEmulator);
            mobile.IsMobileAppTrans.Should().BeTrue(); // Always true
            mobile.Manufacturer.Should().Be(mobileTransaction.Manufacturer);
            mobile.MediaDRMCode.Should().Be(mobileTransaction.MediaDRMCode);
            mobile.ModelName.Should().Be(mobileTransaction.ModelName);
            mobile.OS.Should().Be(mobileTransaction.OS);
            mobile.SdkVersion.Should().Be(mobileTransaction.SdkVersion);
            mobile.SystemLanguage.Should().Be(mobileTransaction.SystemLanguage);
            mobile.Timezone.Should().Be(mobileTransaction.Timezone);
        }

        [Fact]
        public void InsertTransactionAsync_WithAndroidCodeFallback_ShouldUseMachineCode()
        {
            // Arrange
            var mobileTransaction = new MobileTransactionModel
            {
                Account = "fallback-test",
                AndroidCode = null, // Null AndroidCode
                MachineCode = "machine-code-789"
            };

            MessageQueue? capturedMessage = null;
            _mockMassTransitProducer.Setup(x => x.Send(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Callback<MessageQueue, string>((msg, queue) => capturedMessage = msg);

            // Act
            var result = _controller.InsertTransactionAsync(mobileTransaction);

            // Assert
            result.Should().BeOfType<OkResult>();
            capturedMessage.Should().NotBeNull();
            capturedMessage!.Mobile.Should().NotBeNull();
            capturedMessage.Mobile!.MachineCode.Should().Be("machine-code-789");
        }

        [Fact]
        public void InsertTransactionAsync_WithEmptyAndroidCode_ShouldUseMachineCode()
        {
            // Arrange
            var mobileTransaction = new MobileTransactionModel
            {
                Account = "empty-android-test",
                AndroidCode = "", // Empty AndroidCode
                MachineCode = "machine-code-backup"
            };

            MessageQueue? capturedMessage = null;
            _mockMassTransitProducer.Setup(x => x.Send(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Callback<MessageQueue, string>((msg, queue) => capturedMessage = msg);

            // Act
            var result = _controller.InsertTransactionAsync(mobileTransaction);

            // Assert
            result.Should().BeOfType<OkResult>();
            capturedMessage.Should().NotBeNull();
            capturedMessage!.Mobile.Should().NotBeNull();
            capturedMessage.Mobile!.MachineCode.Should().Be("machine-code-backup");
        }

        #endregion

        #region MobileTransactionToBase64 Tests

        [Fact]
        public void MobileTransactionToBase64_WithValidTransaction_ShouldReturnBase64String()
        {
            // Arrange
            var mobileTransaction = CreateValidMobileTransaction();

            // Act
            var result = _controller.MobileTransactionToBase64(mobileTransaction);

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            // Verify it's a valid base64 string
            try
            {
                var decodedBytes = Convert.FromBase64String(result);
                var decodedJson = Encoding.UTF8.GetString(decodedBytes);
                var deserializedTransaction = JsonConvert.DeserializeObject<MobileTransactionModel>(decodedJson);
                
                deserializedTransaction.Should().NotBeNull();
                deserializedTransaction!.Account.Should().Be(mobileTransaction.Account);
                deserializedTransaction.Subscriber.Should().Be(mobileTransaction.Subscriber);
            }
            catch (Exception ex)
            {
                Assert.True(false, $"Result is not a valid base64 string: {ex.Message}");
            }
        }

        [Fact]
        public void MobileTransactionToBase64_WithNullTransaction_ShouldReturnErrorMessage()
        {
            // Arrange
            MobileTransactionModel? transaction = null;

            // Act
            var result = _controller.MobileTransactionToBase64(transaction!);

            // Assert
            result.Should().Be("Invalid transaction data.");
        }

        [Fact]
        public void MobileTransactionToBase64_WithComplexTransaction_ShouldSerializeAllProperties()
        {
            // Arrange
            var mobileTransaction = new MobileTransactionModel
            {
                Account = "complex-test-account",
                Subscriber = "complex-subscriber",
                Action = "complex-action",
                ActionResult = "complex-result",
                AndroidCode = "android-123",
                MachineCode = "machine-456",
                CustomCode = "custom-789",
                MediaDRMCode = "drm-abc",
                IsEmulator = true,
                ModelName = "Test Device",
                Manufacturer = "Test Manufacturer",
                Brand = "Test Brand",
                SdkVersion = "API 33",
                OS = "Android 13",
                Timezone = "UTC+7",
                SystemLanguage = "en-US",
                Country = "US",
                TransDate = new DateTime(2024, 1, 1, 12, 0, 0),
                LoginTime = new DateTime(2024, 1, 1, 11, 59, 0)
            };

            // Act
            var result = _controller.MobileTransactionToBase64(mobileTransaction);

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            // Decode and verify all properties were serialized
            var decodedBytes = Convert.FromBase64String(result);
            var decodedJson = Encoding.UTF8.GetString(decodedBytes);
            var deserializedTransaction = JsonConvert.DeserializeObject<MobileTransactionModel>(decodedJson);
            
            deserializedTransaction.Should().BeEquivalentTo(mobileTransaction);
        }

        [Fact]
        public void MobileTransactionToBase64_WithSpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var mobileTransaction = new MobileTransactionModel
            {
                Account = "test-äöü-account",
                Subscriber = "test-??-subscriber",
                Action = "test-????????",
                AndroidCode = "android-??-code",
                ModelName = "Device with émojis ??"
            };

            // Act
            var result = _controller.MobileTransactionToBase64(mobileTransaction);

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            // Verify special characters are preserved
            var decodedBytes = Convert.FromBase64String(result);
            var decodedJson = Encoding.UTF8.GetString(decodedBytes);
            var deserializedTransaction = JsonConvert.DeserializeObject<MobileTransactionModel>(decodedJson);
            
            deserializedTransaction!.Account.Should().Be("test-äöü-account");
            deserializedTransaction.Subscriber.Should().Be("test-??-subscriber");
            deserializedTransaction.Action.Should().Be("test-????????");
            deserializedTransaction.AndroidCode.Should().Be("android-??-code");
            deserializedTransaction.ModelName.Should().Be("Device with émojis ??");
        }

        [Fact]
        public void MobileTransactionToBase64_ResultShouldBeReversible()
        {
            // Arrange
            var originalTransaction = CreateValidMobileTransaction();

            // Act
            var base64Result = _controller.MobileTransactionToBase64(originalTransaction);
            
            // Reverse the operation
            var decodedBytes = Convert.FromBase64String(base64Result);
            var decodedJson = Encoding.UTF8.GetString(decodedBytes);
            var deserializedTransaction = JsonConvert.DeserializeObject<MobileTransactionModel>(decodedJson);

            // Assert
            deserializedTransaction.Should().BeEquivalentTo(originalTransaction);
        }

        #endregion

        #region Error Handling and Edge Cases

        [Fact]
        public void InsertTransactionAsync_WithHttpContextException_ShouldHandleGracefully()
        {
            // Arrange
            var mobileTransaction = CreateValidMobileTransaction();
            _mockHttpRequest.Setup(x => x.Headers).Throws(new InvalidOperationException("HTTP context error"));

            // Act
            var result = _controller.InsertTransactionAsync(mobileTransaction);

            // Assert
            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }

        [Fact]
        public void InsertTransactionAsync_WithQueueNameException_ShouldLogAndReturnError()
        {
            // Arrange
            var mobileTransaction = CreateValidMobileTransaction();
            _mockAppSettings.Setup(x => x.MobileQueueName).Throws(new Exception("Queue configuration error"));

            // Act
            var result = _controller.InsertTransactionAsync(mobileTransaction);

            // Assert
            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            
            _mockLogger.Verify(x => x.Error(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }

        [Fact]
        public void MobileTransactionToBase64_WithLargeTransaction_ShouldHandleCorrectly()
        {
            // Arrange
            var largeTransaction = new MobileTransactionModel
            {
                Account = new string('A', 10000), // Large account string
                Subscriber = new string('S', 5000),
                AndroidCode = new string('C', 2000),
                ModelName = string.Join("", Enumerable.Repeat("Large Device Name ", 100))
            };

            // Act
            var result = _controller.MobileTransactionToBase64(largeTransaction);

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            // Verify large data is handled correctly
            var decodedBytes = Convert.FromBase64String(result);
            var decodedJson = Encoding.UTF8.GetString(decodedBytes);
            var deserializedTransaction = JsonConvert.DeserializeObject<MobileTransactionModel>(decodedJson);
            
            deserializedTransaction!.Account.Should().HaveLength(10000);
            deserializedTransaction.Subscriber.Should().HaveLength(5000);
        }

        #endregion

        #region Performance and Concurrency Tests

        [Fact]
        public void InsertTransactionAsync_ConcurrentRequests_ShouldHandleCorrectly()
        {
            // Arrange
            const int concurrentRequests = 20;
            var tasks = new List<Task<IActionResult>>();

            // Act
            for (int i = 0; i < concurrentRequests; i++)
            {
                var transaction = CreateValidMobileTransaction();
                transaction.Account = $"concurrent-account-{i}";
                tasks.Add(Task.Run(() => _controller.InsertTransactionAsync(transaction)));
            }

            var results = Task.WhenAll(tasks).Result;

            // Assert
            results.Should().HaveCount(concurrentRequests);
            results.Should().AllBeOfType<OkResult>();

            // Verify all requests were processed
            _mockMassTransitProducer.Verify(x => x.Send(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Exactly(concurrentRequests));
        }

        [Fact]
        public void MobileTransactionToBase64_PerformanceBenchmark_ShouldExecuteQuickly()
        {
            // Arrange
            var mobileTransaction = CreateValidMobileTransaction();
            const int iterations = 1000;
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var result = _controller.MobileTransactionToBase64(mobileTransaction);
                result.Should().NotBeNullOrEmpty(); // Ensure the result is valid
            }

            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, "1000 Base64 conversions should complete within 2 seconds");
            
            // Calculate average time per conversion
            var averageTimePerConversion = (double)stopwatch.ElapsedMilliseconds / iterations;
            averageTimePerConversion.Should().BeLessThan(2.0, "Each conversion should take less than 2ms on average");
        }

        [Fact]
        public void InsertTransactionAsync_HighFrequencyRequests_ShouldMaintainPerformance()
        {
            // Arrange
            const int requestCount = 100;
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < requestCount; i++)
            {
                var transaction = CreateValidMobileTransaction();
                transaction.Account = $"perf-test-{i}";
                var result = _controller.InsertTransactionAsync(transaction);
                result.Should().BeOfType<OkResult>();
            }

            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, "100 insert operations should complete within 3 seconds");
        }

        [Fact]
        public void MobileTransactionToBase64_MemoryUsage_ShouldNotLeakMemory()
        {
            // Arrange
            var mobileTransaction = CreateValidMobileTransaction();

            // Force garbage collection before test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(false);

            // Act
            const int iterations = 5000;
            for (int i = 0; i < iterations; i++)
            {
                var result = _controller.MobileTransactionToBase64(mobileTransaction);
                // Don't hold references to allow garbage collection
            }

            // Force garbage collection after test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            // Memory increase should be minimal (less than 2MB for 5k conversions)
            memoryIncrease.Should().BeLessThan(2 * 1024 * 1024, "Memory usage should not increase significantly");
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void InsertTransactionAsync_EndToEndScenario_ShouldWorkCorrectly()
        {
            // Arrange
            var mobileTransaction = new MobileTransactionModel
            {
                Account = "integration-test-account",
                Subscriber = "integration-subscriber",
                Action = "mobile-payment",
                ActionResult = "success",
                AndroidCode = "integration-android-123",
                Brand = "Samsung",
                ModelName = "Galaxy S24",
                OS = "Android 14",
                Country = "VN",
                TransDate = DateTime.Now
            };

            var forwardedIP = "203.0.113.50";
            _mockHeaders.Setup(x => x["Fps-Forwarded-For"]).Returns(forwardedIP);

            MessageQueue? capturedMessage = null;
            _mockMassTransitProducer.Setup(x => x.Send(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Callback<MessageQueue, string>((msg, queue) => capturedMessage = msg);

            // Act
            var result = _controller.InsertTransactionAsync(mobileTransaction);

            // Assert
            result.Should().BeOfType<OkResult>();
            
            // Verify IP was extracted and set
            mobileTransaction.IP.Should().Be(forwardedIP);
            
            // Verify message was queued correctly
            capturedMessage.Should().NotBeNull();
            capturedMessage!.Account.Should().Be("integration-test-account");
            capturedMessage.IP.Should().Be(forwardedIP);
            capturedMessage.Mobile.Should().NotBeNull();
            capturedMessage.Mobile!.Brand.Should().Be("Samsung");
            capturedMessage.Mobile.IsMobileAppTrans.Should().BeTrue();
            
            // Verify queue was called with correct queue name
            _mockMassTransitProducer.Verify(x => x.Send(capturedMessage, _mockAppSettings.Object.MobileQueueName), Times.Once);
        }

        [Fact]
        public void FullWorkflow_InsertAndSerialize_ShouldWorkTogether()
        {
            // Arrange
            var originalTransaction = CreateValidMobileTransaction();

            // Act - First serialize to Base64
            var base64Result = _controller.MobileTransactionToBase64(originalTransaction);
            
            // Then insert the transaction
            var insertResult = _controller.InsertTransactionAsync(originalTransaction);

            // Assert both operations
            base64Result.Should().NotBeNullOrEmpty();
            insertResult.Should().BeOfType<OkResult>();

            // Verify Base64 is valid
            var decodedBytes = Convert.FromBase64String(base64Result);
            var decodedJson = Encoding.UTF8.GetString(decodedBytes);
            var deserializedTransaction = JsonConvert.DeserializeObject<MobileTransactionModel>(decodedJson);
            
            // Compare excluding IP property since Base64 serialization happens before IP is set
            deserializedTransaction.Should().BeEquivalentTo(originalTransaction, options => options.Excluding(x => x.IP));
        }

        #endregion

        #region Helper Methods

        private static MobileTransactionModel CreateValidMobileTransaction()
        {
            return new MobileTransactionModel
            {
                Account = "test-mobile-account",
                Subscriber = "test-mobile-subscriber",
                Action = "mobile-login",
                ActionResult = "success",
                AndroidCode = "android-test-123",
                MachineCode = "machine-test-456",
                CustomCode = "custom-test-789",
                MediaDRMCode = "drm-test-abc",
                IsEmulator = false,
                ModelName = "Test Device",
                Manufacturer = "Test Manufacturer",
                Brand = "Test Brand",
                SdkVersion = "API 34",
                OS = "Android 14",
                Timezone = "Asia/Ho_Chi_Minh",
                SystemLanguage = "vi-VN",
                Country = "VN",
                TransDate = DateTime.Now,
                LoginTime = DateTime.Now.AddMinutes(-1)
            };
        }

        #endregion
    }
}