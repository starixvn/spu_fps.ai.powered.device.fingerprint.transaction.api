using Device.FingerPrint.Models;
using Device.FingerPrint.TransactionAPI.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Services
{
    public class FingerprintServiceTests
    {
        private readonly Mock<IRedisMultiplexDbService> _redisServiceMock;
        private readonly Mock<ILinkingService> _linkingServiceMock;

        private readonly FingerprintService _fingerprintService;

        public FingerprintServiceTests()
        {
            var appSettingsMock = new Mock<IAppSettings>();
            appSettingsMock.Setup(a => a.RedisSettings.FingerprintTTLInSeconds).Returns(3600);
            _redisServiceMock = new Mock<IRedisMultiplexDbService>();
            _linkingServiceMock = new Mock<ILinkingService>();
            _fingerprintService = new FingerprintService(
                appSettingsMock.Object,
                _redisServiceMock.Object,
                _linkingServiceMock.Object);
        }

        [Fact]
        public async Task InsertFP4_ShouldInsertFingerprintAndCallLinkingService()
        {
            // Arrange
            var fingerprintRequest = new FingerprintRequest
            {
                Hashed = "hashed_fp4",
                Fingerprint = new APFingerprintModel { Audio = "audio", Browser = "browser", Hardware ="hardware", Preferences = "preferences" },
                SessionKey = "session_key_1"
            };
            // Act
            var result = await _fingerprintService.InsertFP4(fingerprintRequest);
            // Assert
            Assert.Equal(fingerprintRequest.Hashed, result);
            _redisServiceMock.Verify(r => r.SetAndExpireAsync(
                It.IsAny<StackExchange.Redis.IDatabase>(),
                fingerprintRequest.Hashed,
                fingerprintRequest.Fingerprint,
                3600), Times.Once);
            _linkingServiceMock.Verify(l => l.LinkingOnInsert(fingerprintRequest.SessionKey), Times.Once);
        }

        [Fact]
        public async Task InsertFP5_ShouldInsertFingerprintAndCallLinkingService()
        {
            // Arrange
            var fingerprintRequest = new FingerprintRequest
            {
                Hashed = "hashed_fp5",
                Fingerprint = new APFingerprintModel { Graphic = "Graphic" },
                SessionKey = "session_key_2"
            };
            // Act
            var result = await _fingerprintService.InsertFP5(fingerprintRequest);
            // Assert
            Assert.Equal(fingerprintRequest.Hashed, result);
            _redisServiceMock.Verify(r => r.SetAndExpireAsync(
                It.IsAny<StackExchange.Redis.IDatabase>(),
                fingerprintRequest.Hashed,
                fingerprintRequest.Fingerprint,
                3600), Times.Once);
            _linkingServiceMock.Verify(l => l.LinkingOnInsert(fingerprintRequest.SessionKey), Times.Once);
        }
    }
}
