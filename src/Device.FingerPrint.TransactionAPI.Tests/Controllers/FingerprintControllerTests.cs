using Device.FingerPrint.TransactionAPI.Controllers;
using Device.FingerPrint.TransactionAPI.Model;
using Device.FingerPrint.TransactionAPI.Services;
using Fanex.Logging;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Controllers
{
    public class FingerprintControllerTests
    {
        private readonly Mock<ILogger> mockLogger;
        private readonly Mock<IRedisService> mockRedisService;
        private readonly Mock<ILinkingService> mockLinkingService;
        private readonly FingerprintController controller;

        public FingerprintControllerTests()
        {
            mockLogger = new Mock<ILogger>();
            mockRedisService = new Mock<IRedisService>();
            mockLinkingService = new Mock<ILinkingService>();
            controller = new FingerprintController(mockLogger.Object, mockRedisService.Object, mockLinkingService.Object);
        }

        [Fact]
        public async Task InsertFP4_ShouldReturnOk()
        {
            // Arrange
            var fingerprintRequest = new FingerprintRequest() { SessionKey = "test-session-key", Hashed = "Hashed", Fingerprint = new APFingerprintModel()};

            // Act
            var result = await controller.InsertFP4(fingerprintRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task InsertFP5_ShouldReturnOk()
        {
            // Arrange
            var fingerprintRequest = new FingerprintRequest() { SessionKey = "test-session-key", Hashed = "Hashed", Fingerprint = new APFingerprintModel() };

            // Act
            var result = await controller.InsertFP5(fingerprintRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
