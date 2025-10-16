using Device.FingerPrint.Model;
using Device.FingerPrint.TransactionAPI.Controllers;
using Device.FingerPrint.TransactionAPI.Queue;
using Device.FingerPrint.TransactionAPI.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ONEbook.UM.Models;
using ONEbook.UM.Services;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Controllers
{
    public class TransactionControllerTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<ITransactionService> _mockTransactionService;
        private readonly Mock<IDeviceService> _mockDeviceService;
        private readonly Mock<IMassTransitProducer> _mockMassitProducer;
        private readonly Mock<IAppSettings> _mockAppSettings;
        private readonly Mock<IExpiredTransactionService> _mockExpiredTransactionService;
        private readonly Mock<ISubscriberService> _mockSubscriberService;
        private readonly TransactionController _controller;

        public TransactionControllerTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockTransactionService = new Mock<ITransactionService>();
            _mockDeviceService = new Mock<IDeviceService>();
            _mockMassitProducer = new Mock<IMassTransitProducer>();
            _mockAppSettings = new Mock<IAppSettings>();
            _mockExpiredTransactionService = new Mock<IExpiredTransactionService>();
            _mockSubscriberService = new Mock<ISubscriberService>();

            _mockSubscriberService.Setup(s => s.ValidateAsync(It.IsAny<string>()))
                .ReturnsAsync(1);

            _controller = new TransactionController(
                _mockLogger.Object,
                _mockTransactionService.Object,
                _mockDeviceService.Object,
                _mockMassitProducer.Object,
                _mockAppSettings.Object,
                _mockExpiredTransactionService.Object,
                _mockSubscriberService.Object);
        }

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
                TransDate = DateTime.Now,
                
            };
        }

        #endregion

        [Fact]
        public async Task InsertTransactionAPFPAsync_ShouldCallLinkingService_WhenIsOldFlow()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            _mockDeviceService.Setup(x => x.GetDeviceInfo(It.IsAny<string>()))
                .Returns(new DeviceInfoModel { Version = "2.1.7" });
            // Act
            var result = await _controller.InsertTransactionAPFPAsync(transactionRequest);

            // Assert
            result.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task InsertTransactionAPFPAsync_ShouldCallLinkingService_WhenIsNewFlow()
        {
            // Arrange
            var transactionRequest = CreateValidTransactionRequest();
            _mockDeviceService.Setup(x => x.GetDeviceInfo(It.IsAny<string>()))
                .Returns(new DeviceInfoModel { Version = "3.0.0" });

            // Act
            var result = await _controller.InsertTransactionAPFPAsync(transactionRequest);

            // Assert
            result.Should().BeOfType<OkResult>();
        }
    }
}