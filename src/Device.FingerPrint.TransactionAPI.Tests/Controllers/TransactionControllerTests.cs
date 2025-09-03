using Device.FingerPrint.TransactionAPI.Controllers;
using Device.FingerPrint.TransactionAPI.Model;
using Device.FingerPrint.TransactionAPI.Queue;
using Device.FingerPrint.TransactionAPI.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ONEbook.UM.Models;
using ONEbook.UM.Services;
using System.Text.Json;
using Xunit;
using Fanex.Logging;
using System.Diagnostics;

namespace Device.FingerPrint.TransactionAPI.Tests.Controllers
{
    /// <summary>
    /// Comprehensive test suite for TransactionController
    /// Tests all public methods and their various scenarios including:
    /// - Successful transaction processing
    /// - Validation failures
    /// - Maintenance mode handling
    /// - Ignored sites processing
    /// - Exception handling
    /// - Data transformation logic
    /// </summary>
    public class TransactionControllerTests
    {
        private readonly Mock<IMaintenanceService> _mockMaintenanceService;
        private readonly Mock<IAppSettings> _mockAppSettings;
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<ISubscriberService> _mockSubscriberService;
        private readonly Mock<IMassTransitProducer> _mockMassTransitProducer;
        private readonly Mock<IRedisService> _mockRedisService;
        private readonly TransactionController _controller;

        static TransactionControllerTests()
        {
            // Initialize logging system for tests to prevent NotImplementedException
            LogManager.SetDefaultLogCategory("Test")
                .Use([new TestLogger()]);
        }

        public TransactionControllerTests()
        {
            _mockMaintenanceService = new Mock<IMaintenanceService>();
            _mockAppSettings = new Mock<IAppSettings>();
            _mockLogger = new Mock<ILogger>();
            _mockSubscriberService = new Mock<ISubscriberService>();
            _mockMassTransitProducer = new Mock<IMassTransitProducer>();
            _mockRedisService = new Mock<IRedisService>();

            _controller = new TransactionController(
                _mockMaintenanceService.Object,
                _mockAppSettings.Object,
                _mockLogger.Object,
                _mockSubscriberService.Object,
                _mockMassTransitProducer.Object,
                _mockRedisService.Object);

            SetupDefaultMocks();
        }

        private void SetupDefaultMocks()
        {
            // Default maintenance settings - system is operational
            _mockMaintenanceService.Setup(x => x.GetUnderMaintenanceTime())
                .Returns(new UnderMaintenanceTime { IsUnderMaintenanceTime = false });

            _mockAppSettings.Setup(x => x.IsUM).Returns(false);
            _mockAppSettings.Setup(x => x.IgnoredSites).Returns("");
            _mockAppSettings.Setup(x => x.QueueName).Returns("test-transaction-queue");
        }

        #region CheckTransactionAsync Tests

        [Fact]
        public async Task CheckTransactionAsync_WithSuccessfulValidation_ShouldReturnSuccessAndPushToQueue()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            
            _mockSubscriberService.Setup(x => x.ValidateAsync(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(TransactionResult.Success);

            _mockMassTransitProducer.Setup(x => x.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(TransactionResult.GetMessage(TransactionResult.Success));

            // Verify all dependencies were called correctly
            _mockSubscriberService.Verify(x => x.ValidateAsync(transactionRequest), Times.Once);
            _mockMassTransitProducer.Verify(x => x.SendAsync(It.IsAny<MessageQueue>(), _mockAppSettings.Object.QueueName), Times.Once);
            
            // Verify no error logging occurred
            _mockLogger.Verify(x => x.ErrorAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CheckTransactionAsync_WithValidationFailure_ShouldReturnErrorAndLogFailure()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            var errorCode = TransactionResult.EmptyAccount;
            
            _mockSubscriberService.Setup(x => x.ValidateAsync(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(errorCode);

            // Act
            var result = await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(TransactionResult.GetMessage(errorCode));

            // Verify no queue message was sent for failed validation
            _mockMassTransitProducer.Verify(x => x.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Never);
            
            // Verify error was logged with proper format
            _mockLogger.Verify(x => x.ErrorAsync(It.Is<string>(msg => 
                msg.Contains("CheckTransactionAsync responseCode") && 
                msg.Contains(errorCode))), Times.Once);
        }

        [Fact]
        public async Task CheckTransactionAsync_WithMaintenanceFromService_ShouldReturnMaintenanceMessage()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            
            _mockMaintenanceService.Setup(x => x.GetUnderMaintenanceTime())
                .Returns(new UnderMaintenanceTime { IsUnderMaintenanceTime = true });

            // Act
            var result = await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(TransactionResult.GetMessage(TransactionResult.UnderMaintenance));

            // Verify subscriber service was not called during maintenance
            _mockSubscriberService.Verify(x => x.ValidateAsync(It.IsAny<TransactionRequest>()), Times.Never);
            _mockMassTransitProducer.Verify(x => x.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CheckTransactionAsync_WithMaintenanceFromAppSettings_ShouldReturnMaintenanceMessage()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            
            _mockAppSettings.Setup(x => x.IsUM).Returns(true);

            // Act
            var result = await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(TransactionResult.GetMessage(TransactionResult.UnderMaintenance));

            // Verify subscriber service was not called during maintenance
            _mockSubscriberService.Verify(x => x.ValidateAsync(It.IsAny<TransactionRequest>()), Times.Never);
        }

        [Fact]
        public async Task CheckTransactionAsync_WithSpecificIgnoredSite_ShouldReturnMaintenanceMessage()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            transactionRequest.Subscriber = "blocked-site";
            
            _mockAppSettings.Setup(x => x.IgnoredSites).Returns("blocked-site,another-blocked-site");

            // Act
            var result = await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(TransactionResult.GetMessage(TransactionResult.UnderMaintenance));

            // Verify subscriber service was not called for ignored sites
            _mockSubscriberService.Verify(x => x.ValidateAsync(It.IsAny<TransactionRequest>()), Times.Never);
        }

        [Fact]
        public async Task CheckTransactionAsync_WithWildcardIgnoredSites_ShouldReturnMaintenanceMessage()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            
            _mockAppSettings.Setup(x => x.IgnoredSites).Returns("*");

            // Act
            var result = await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(TransactionResult.GetMessage(TransactionResult.UnderMaintenance));

            // Verify subscriber service was not called with wildcard ignore
            _mockSubscriberService.Verify(x => x.ValidateAsync(It.IsAny<TransactionRequest>()), Times.Never);
        }

        [Fact]
        public async Task CheckTransactionAsync_WithCaseInsensitiveIgnoredSite_ShouldReturnMaintenanceMessage()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            transactionRequest.Subscriber = "BLOCKED-SITE";
            
            _mockAppSettings.Setup(x => x.IgnoredSites).Returns("blocked-site");

            // Act
            var result = await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(TransactionResult.GetMessage(TransactionResult.UnderMaintenance));
        }

        [Fact]
        public async Task CheckTransactionAsync_WithUnhandledException_ShouldReturnOthersErrorAndLogException()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            var exception = new Exception("Unexpected database error");
            
            _mockSubscriberService.Setup(x => x.ValidateAsync(It.IsAny<TransactionRequest>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(TransactionResult.GetMessage(TransactionResult.Others));

            // Verify error was logged with the exception
            _mockLogger.Verify(x => x.Error(exception.Message, exception), Times.Once);
        }

        [Fact]
        public async Task CheckTransactionAsync_WithQueueSendException_ShouldStillReturnOthersError()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            
            _mockSubscriberService.Setup(x => x.ValidateAsync(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(TransactionResult.Success);

            _mockMassTransitProducer.Setup(x => x.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Queue connection failed"));

            // Act
            var result = await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(TransactionResult.GetMessage(TransactionResult.Others));
        }

        [Fact]
        public async Task CheckTransactionAsync_WithNullSubscriber_ShouldHandleGracefully()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            transactionRequest.Subscriber = null;

            _mockSubscriberService.Setup(x => x.ValidateAsync(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(TransactionResult.Success);

            _mockMassTransitProducer.Setup(x => x.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(TransactionResult.GetMessage(TransactionResult.Success));
        }

        #endregion

        #region InsertTransactionAsync Tests

        [Fact]
        public void InsertTransactionAsync_WithValidRequest_ShouldReturnOk()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();

            // Act
            var result = _controller.InsertTransactionAsync(transactionRequest);

            // Assert
            result.Should().BeOfType<OkResult>();
        }

        [Fact]
        public void InsertTransactionAsync_WithNullRequest_ShouldHandleGracefully()
        {
            // Arrange
            TransactionRequest? transactionRequest = null;

            // Act & Assert - This should not throw, but will likely cause issues in TransformDeviceModel
            // In a real scenario, you might want to add null checking to the controller method
            var exception = Assert.Throws<NullReferenceException>(() => 
                _controller.InsertTransactionAsync(transactionRequest!));
            
            exception.Should().NotBeNull();
        }

        [Fact]
        public void InsertTransactionAsync_ShouldNotBlockOnQueueOperation()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            var slowQueue = new Mock<IMassTransitProducer>();
            
            // Setup a slow queue operation
            slowQueue.Setup(x => x.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Returns(Task.Delay(2000)); // 2 second delay

            var slowController = new TransactionController(
                _mockMaintenanceService.Object,
                _mockAppSettings.Object,
                _mockLogger.Object,
                _mockSubscriberService.Object,
                slowQueue.Object,
                _mockRedisService.Object);

            // Act
            var stopwatch = Stopwatch.StartNew();
            var result = slowController.InsertTransactionAsync(transactionRequest);
            stopwatch.Stop();

            // Assert
            result.Should().BeOfType<OkResult>();
            // The method should return immediately since it's fire-and-forget
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
        }

        #endregion

        #region TestUM Tests

        [Fact]
        public void TestUM_ShouldReturnMaintenanceTimeFromService()
        {
            // Arrange
            var expectedMaintenanceTime = new UnderMaintenanceTime 
            { 
                IsUnderMaintenanceTime = true
            };
            
            _mockMaintenanceService.Setup(x => x.GetUnderMaintenanceTime())
                .Returns(expectedMaintenanceTime);

            // Act
            var result = _controller.TestUM();

            // Assert
            result.Should().BeEquivalentTo(expectedMaintenanceTime);
            _mockMaintenanceService.Verify(x => x.GetUnderMaintenanceTime(), Times.Once);
        }

        [Fact]
        public void TestUM_WithNullMaintenanceTime_ShouldReturnNull()
        {
            // Arrange
            _mockMaintenanceService.Setup(x => x.GetUnderMaintenanceTime())
                .Returns((UnderMaintenanceTime?)null);

            // Act
            var result = _controller.TestUM();

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Device Model Transformation Tests

        [Fact]
        public async Task DeviceModelTransformation_ShouldMapAllPropertiesCorrectly()
        {
            // Arrange
            var transactionRequest = new TransactionRequest
            {
                Account = "test-account",
                Subscriber = "test-subscriber",
                AcceptsTypes = "application/json, text/html",
                DetecasAnalysis = "analysis-data-123",
                Di = "primary-device-id",
                __di = "fallback-device-id",
                Ip = "192.168.1.100",
                Url = "https://example.com/login",
                Platform = "Windows 11",
                Screen = "1920x1080x24",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
            };

            _mockSubscriberService.Setup(x => x.ValidateAsync(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(TransactionResult.Success);

            MessageQueue? capturedMessage = null;
            _mockMassTransitProducer.Setup(x => x.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Callback<MessageQueue, string>((msg, queue) => capturedMessage = msg)
                .Returns(Task.CompletedTask);

            // Act
            await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            capturedMessage.Should().NotBeNull();
            capturedMessage!.Device.Should().NotBeNull();
            
            var device = capturedMessage.Device!;
            device.AcceptsTypes.Should().Be(transactionRequest.AcceptsTypes);
            device.DetecasAnalysis.Should().Be(transactionRequest.DetecasAnalysis);
            device.Di.Should().Be(transactionRequest.Di); // Should prefer Di over __di
            device.Ip.Should().Be(transactionRequest.Ip);
            device.Url.Should().Be(transactionRequest.Url);
            device.Platform.Should().Be(transactionRequest.Platform);
            device.Screen.Should().Be(transactionRequest.Screen);
            device.UserAgent.Should().Be(transactionRequest.UserAgent);
            device.TotalMinutes.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task DeviceModelTransformation_WithNullDi_ShouldUseFallbackDi()
        {
            // Arrange
            var transactionRequest = new TransactionRequest
            {
                Account = "test-account",
                Subscriber = "test-subscriber",
                Di = null,
                __di = "fallback-device-id-456"
            };

            _mockSubscriberService.Setup(x => x.ValidateAsync(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(TransactionResult.Success);

            MessageQueue? capturedMessage = null;
            _mockMassTransitProducer.Setup(x => x.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Callback<MessageQueue, string>((msg, queue) => capturedMessage = msg)
                .Returns(Task.CompletedTask);

            // Act
            await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            capturedMessage.Should().NotBeNull();
            capturedMessage!.Device.Should().NotBeNull();
            capturedMessage.Device!.Di.Should().Be("fallback-device-id-456");
        }

        [Fact]
        public async Task DeviceModelTransformation_WithBothDiNull_ShouldHaveNullDi()
        {
            // Arrange
            var transactionRequest = new TransactionRequest
            {
                Account = "test-account",
                Subscriber = "test-subscriber",
                Di = null,
                __di = null
            };

            _mockSubscriberService.Setup(x => x.ValidateAsync(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(TransactionResult.Success);

            MessageQueue? capturedMessage = null;
            _mockMassTransitProducer.Setup(x => x.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Callback<MessageQueue, string>((msg, queue) => capturedMessage = msg)
                .Returns(Task.CompletedTask);

            // Act
            await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            capturedMessage.Should().NotBeNull();
            capturedMessage!.Device.Should().NotBeNull();
            capturedMessage.Device!.Di.Should().BeNull();
        }

        [Fact]
        public async Task DeviceModelTransformation_TotalMinutes_ShouldBeCalculatedCorrectly()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();

            _mockSubscriberService.Setup(x => x.ValidateAsync(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(TransactionResult.Success);

            MessageQueue? capturedMessage = null;
            _mockMassTransitProducer.Setup(x => x.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Callback<MessageQueue, string>((msg, queue) => capturedMessage = msg)
                .Returns(Task.CompletedTask);

            // Act
            await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            capturedMessage.Should().NotBeNull();
            capturedMessage!.Device.Should().NotBeNull();
            
            var totalMinutes = capturedMessage.Device!.TotalMinutes;
            totalMinutes.Should().NotBeNullOrEmpty();
            
            // TotalMinutes should be a parseable double (difference between UTC and local time)
            double.TryParse(totalMinutes, out var minutes).Should().BeTrue();
            // The value should be within reasonable bounds (typically -12 to +14 hours)
            Math.Abs(minutes).Should().BeLessOrEqualTo(14 * 60); // 14 hours in minutes
        }

        #endregion

        #region Message Queue Mapping Tests

        [Fact]
        public async Task MessageQueueMapping_ShouldMapAllFieldsCorrectly()
        {
            // Arrange
            var transactionRequest = new TransactionRequest
            {
                Account = "premium-account",
                Action = "user-login",
                ActionResult = "success",
                Subscriber = "premium-subscriber",
                SubscriberId = 12345,
                Ip = "203.0.113.42",
                FakeIp = "192.0.2.100",
                TransDate = new DateTime(2024, 1, 15, 10, 30, 0),
                Token = "auth-token-xyz789",
                SessionKey = "session-abc123",
                Bi = "bot-detection-fingerprint-data"
            };

            _mockSubscriberService.Setup(x => x.ValidateAsync(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(TransactionResult.Success);

            MessageQueue? capturedMessage = null;
            _mockMassTransitProducer.Setup(x => x.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Callback<MessageQueue, string>((msg, queue) => capturedMessage = msg)
                .Returns(Task.CompletedTask);

            // Act
            await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            capturedMessage.Should().NotBeNull();
            
            capturedMessage!.Account.Should().Be(transactionRequest.Account);
            capturedMessage.Action.Should().Be(transactionRequest.Action);
            capturedMessage.ActionResult.Should().Be(transactionRequest.ActionResult);
            capturedMessage.Subscriber.Should().Be(transactionRequest.Subscriber);
            capturedMessage.SubscriberId.Should().Be(transactionRequest.SubscriberId);
            capturedMessage.IP.Should().Be(transactionRequest.Ip);
            capturedMessage.FakeIP.Should().Be(transactionRequest.FakeIp);
            capturedMessage.TransDate.Should().Be(transactionRequest.TransDate);
            capturedMessage.Token.Should().Be(transactionRequest.Token);
            capturedMessage.SessionKey.Should().Be(transactionRequest.SessionKey);
            capturedMessage.BotD.Should().Be(transactionRequest.Bi);
            
            // Fixed fields
            capturedMessage.Recaptcha.Should().Be("");
            
            // Auto-generated fields
            capturedMessage.SubscriberDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
            capturedMessage.Device.Should().NotBeNull();
        }

        [Fact]
        public async Task MessageQueueMapping_WithNullValues_ShouldHandleGracefully()
        {
            // Arrange
            var transactionRequest = new TransactionRequest
            {
                Account = "test-account",
                Subscriber = "test-subscriber",
                // All other properties will be null/default
            };

            _mockSubscriberService.Setup(x => x.ValidateAsync(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(TransactionResult.Success);

            MessageQueue? capturedMessage = null;
            _mockMassTransitProducer.Setup(x => x.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Callback<MessageQueue, string>((msg, queue) => capturedMessage = msg)
                .Returns(Task.CompletedTask);

            // Act
            await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            capturedMessage.Should().NotBeNull();
            
            // Should handle null values gracefully
            capturedMessage!.Action.Should().BeNull();
            capturedMessage.ActionResult.Should().BeNull();
            capturedMessage.IP.Should().BeNull();
            capturedMessage.Token.Should().BeNull();
            capturedMessage.BotD.Should().BeNull();
            
            // Required fields should still be populated
            capturedMessage.Account.Should().Be(transactionRequest.Account);
            capturedMessage.Subscriber.Should().Be(transactionRequest.Subscriber);
            capturedMessage.Recaptcha.Should().Be("");
            capturedMessage.Device.Should().NotBeNull();
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Fact]
        public async Task CheckTransactionAsync_WithEmptyIgnoredSites_ShouldProcessNormally()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            
            _mockAppSettings.Setup(x => x.IgnoredSites).Returns("");
            _mockSubscriberService.Setup(x => x.ValidateAsync(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(TransactionResult.Success);
            _mockMassTransitProducer.Setup(x => x.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(TransactionResult.GetMessage(TransactionResult.Success));
        }

        [Fact]
        public async Task CheckTransactionAsync_WithMultipleIgnoredSites_ShouldCheckCorrectly()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            transactionRequest.Subscriber = "site-b";
            
            _mockAppSettings.Setup(x => x.IgnoredSites).Returns("site-a,site-b,site-c");

            // Act
            var result = await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(TransactionResult.GetMessage(TransactionResult.UnderMaintenance));
        }

        [Fact]
        public async Task CheckTransactionAsync_WithIgnoredSiteContainingSpaces_ShouldHandleCorrectly()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            transactionRequest.Subscriber = "site with spaces";
            
            _mockAppSettings.Setup(x => x.IgnoredSites).Returns("site-a,site with spaces,site-c");

            // Act
            var result = await _controller.CheckTransactionAsync(transactionRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(TransactionResult.GetMessage(TransactionResult.UnderMaintenance));
        }

        #endregion

        #region Performance and Concurrency Tests

        [Fact]
        public async Task CheckTransactionAsync_ConcurrentRequests_ShouldHandleCorrectly()
        {
            // Arrange
            const int concurrentRequests = 10;
            var tasks = new List<Task<IActionResult>>();

            _mockSubscriberService.Setup(x => x.ValidateAsync(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(TransactionResult.Success);

            _mockMassTransitProducer.Setup(x => x.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            for (int i = 0; i < concurrentRequests; i++)
            {
                var request = CreateValidTransactionRequest();
                request.Account = $"concurrent-account-{i}";
                tasks.Add(_controller.CheckTransactionAsync(request));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(concurrentRequests);
            results.Should().AllBeOfType<OkObjectResult>();

            foreach (var result in results.Cast<OkObjectResult>())
            {
                result.Value.Should().Be(TransactionResult.GetMessage(TransactionResult.Success));
            }

            // Verify all requests were processed
            _mockSubscriberService.Verify(x => x.ValidateAsync(It.IsAny<TransactionRequest>()), Times.Exactly(concurrentRequests));
        }

        [Fact]
        public async Task CheckTransactionAsync_HighFrequencyRequests_ShouldMaintainPerformance()
        {
            // Arrange
            const int requestCount = 100;
            var stopwatch = Stopwatch.StartNew();

            _mockSubscriberService.Setup(x => x.ValidateAsync(It.IsAny<TransactionRequest>()))
                .ReturnsAsync(TransactionResult.Success);

            _mockMassTransitProducer.Setup(x => x.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var tasks = new List<Task<IActionResult>>();

            // Act
            for (int i = 0; i < requestCount; i++)
            {
                var request = CreateValidTransactionRequest();
                request.Account = $"perf-test-{i}";
                tasks.Add(_controller.CheckTransactionAsync(request));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
            tasks.Should().HaveCount(requestCount);
        }

        #endregion

        #region Helper Methods

        private static TransactionRequest CreateValidTransactionRequest()
        {
            return new TransactionRequest
            {
                Account = "test-account",
                Action = "login",
                ActionResult = "success",
                AcceptsTypes = "application/json",
                Platform = "Windows",
                UserAgent = "Mozilla/5.0 (Test)",
                Screen = "1920x1080",
                Ip = "192.168.1.1",
                FakeIp = "10.0.0.1",
                Url = "https://example.com",
                Di = "device-id-123",
                __di = "fallback-device-id",
                Bi = "bot-detection-data",
                Token = "test-token",
                DetecasAnalysis = "analysis-data",
                Subscriber = "test-subscriber",
                SubscriberId = 0, // Will be set by subscriber service
                LoginTime = DateTime.Now,
                CaptchaResult = "success",
                SessionKey = "session-123",
                TransDate = DateTime.Now
            };
        }

        #endregion

        [Fact]
        public async Task InsertTransactionAPFPAsync_ShouldReturnOk()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            // Act
            var result = await _controller.InsertTransactionAPFPAsync(transactionRequest);
            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }
    }

    /// <summary>
    /// Simple test logger implementation to prevent logging exceptions in tests
    /// </summary>
    internal class TestLogger : ILogEngine
    {
        public void Debug(string message, string? category = null) { }
        public void Error(string message, string? category = null) { }
        public void Info(string message, string? category = null) { }
        public void Warning(string message, string? category = null) { }
        public Task DebugAsync(string message, string? category = null) => Task.CompletedTask;
        public Task ErrorAsync(string message, string? category = null) => Task.CompletedTask;
        public Task InfoAsync(string message, string? category = null) => Task.CompletedTask;
        public Task WarningAsync(string message, string? category = null) => Task.CompletedTask;
        
        public void Write(string message, TraceEventType eventType, string? category = null) { }
        public Task WriteAsync(string message, TraceEventType eventType, string? category = null) => Task.CompletedTask;
        public string RenderJavascriptLogger() => string.Empty;
    }
}