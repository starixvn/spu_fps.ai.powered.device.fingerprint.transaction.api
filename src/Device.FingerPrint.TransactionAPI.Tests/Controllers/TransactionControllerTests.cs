using Device.FingerPrint.TransactionAPI.Controllers;
using Device.FingerPrint.TransactionAPI.Model;
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
        private readonly Mock<IMaintenanceService> _mockMaintenanceService;
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<ILinkingService> _mockLinkingService;
        private readonly TransactionController _controller;

        public TransactionControllerTests()
        {
            _mockMaintenanceService = new Mock<IMaintenanceService>();
            _mockLogger = new Mock<ILogger>();
            _mockLinkingService = new Mock<ILinkingService>();

            _controller = new TransactionController(
                _mockMaintenanceService.Object,
                _mockLogger.Object,
                _mockLinkingService.Object);
        }


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
}