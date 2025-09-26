using Device.FingerPrint.Models;
using Device.FingerPrint.TransactionAPI.Queue;
using Device.FingerPrint.TransactionAPI.Services;
using FluentAssertions;
using Moq;
using StackExchange.Redis;
using System.Text.Json;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Services
{
    public class LinkingServiceTests
    {
        private readonly Mock<IAppSettings> _mockAppSettings;
        private readonly Mock<IRedisMultiplexDbService> _mockRedisService;
        private readonly Mock<IDatabase> _mockRedisDatabase;
        private readonly Mock<IMassTransitProducer> _mockMassTransitProducer;

        private readonly LinkingService _linkingService;

        private readonly APFingerprintModel fp4MockData = new APFingerprintModel()
        {
            Browser = "Chrome",
            Audio = "AudioData",
            Hardware = "HardwareData",
            Preferences = "PreferencesData"
        };

        private readonly APFingerprintModel fp5MockData = new APFingerprintModel()
        {
            Graphic = "GraphicData",
        };

        private readonly TransactionModel transactionMockData = new TransactionModel()
        {
            Account = "testAccount",
            Di = "testDi",
            __di = "test__di",
        };

        public LinkingServiceTests()
        {
            _mockAppSettings = new Mock<IAppSettings>();
            _mockRedisService = new Mock<IRedisMultiplexDbService>();
            _mockRedisDatabase = new Mock<IDatabase>();
            _mockMassTransitProducer = new Mock<IMassTransitProducer>();

            var redisSettings = new RedisSettings
            {
                FingerprintTTLInSeconds = 10800,
                TransactionTTLInSeconds = 90
            };
            _mockAppSettings.Setup(r => r.RedisSettings).Returns(redisSettings);

            // Setup the Redis service to return the mock database
            _mockRedisService.Setup(r => r.ShadowDatabase).Returns(_mockRedisDatabase.Object);
            _mockRedisService.Setup(r => r.FP4Database).Returns(_mockRedisDatabase.Object);
            _mockRedisService.Setup(r => r.FP5Database).Returns(_mockRedisDatabase.Object);
            _mockRedisService.Setup(r => r.TransactionDatabase).Returns(_mockRedisDatabase.Object);
            _linkingService = new LinkingService(_mockAppSettings.Object, _mockRedisService.Object, _mockMassTransitProducer.Object);
        }

        [Fact]
        public async Task LinkingOnInsert_ShouldReturnFalse_WhenTransactionNotFound()
        {
            // Arrange
            var transactionJson = JsonSerializer.Serialize(transactionMockData);

            _mockRedisService.Setup(db => db.TransactionDatabase.SortedSetRangeByRankAsync(It.IsAny<RedisKey>(), 0, -1, Order.Ascending, CommandFlags.None))
                .Returns(Task.FromResult(Array.Empty<RedisValue>()));
            // Act
            var result = await _linkingService.LinkingOnInsert("testSession");

            // Assert
            result.Should().BeFalse();
            _mockMassTransitProducer.Verify(m => m.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void BuildMessageQueue_ShouldReturnCorrectModel()
        {
            // Arrange
            var expected = new APFingerprintModel
            {
                Audio = fp4MockData.Audio,
                Browser = fp4MockData.Browser,
                Graphic = fp5MockData.Graphic,
                Hardware = fp4MockData.Hardware,
                Preferences = fp4MockData.Preferences
            };

            // Act
            var actual = _linkingService.BuildMessageQueue(transactionMockData, fp4MockData, fp5MockData);

            // Assert
            actual.Should().NotBeNull();
            actual.Fingerprint.Should().BeEquivalentTo(expected);
            actual.Account.Should().BeEquivalentTo(transactionMockData.Account);
        }

        [Fact]
        public void LinkFingerprintsAttributes_ShouldReturnCorrectModel()
        {
            // Arrange
            var expected = new APFingerprintModel
            {
                Audio = fp4MockData.Audio,
                Browser = fp4MockData.Browser,
                Graphic = fp5MockData.Graphic,
                Hardware = fp4MockData.Hardware,
                Preferences = fp4MockData.Preferences
            };

            // Act
            var actual = _linkingService.LinkFingerprintsAttributes([fp4MockData, fp5MockData]);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void LinkFingerprintsAttributes_WhenMissingFP5_ShouldReturnCorrectModel()
        {
            // Arrange
            var expected = new APFingerprintModel
            {
                Audio = fp4MockData.Audio,
                Browser = fp4MockData.Browser,
                Hardware = fp4MockData.Hardware,
                Preferences = fp4MockData.Preferences
            };

            // Act
            var actual = _linkingService.LinkFingerprintsAttributes([fp4MockData]);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void LinkFingerprintsAttributes_WhenMissingFP4_ShouldReturnCorrectModel()
        {
            // Arrange
            var expected = new APFingerprintModel
            {
                Graphic = fp5MockData.Graphic,
            };

            // Act
            var actual = _linkingService.LinkFingerprintsAttributes([fp5MockData]);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }
    }
}
