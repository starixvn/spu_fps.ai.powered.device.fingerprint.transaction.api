using Device.FingerPrint.TransactionAPI.Model;
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
        private readonly Mock<IBatch> _mockBatch;
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

        private readonly TransactionRequest transactionMockData = new TransactionRequest()
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
            _mockBatch = new Mock<IBatch>();
            _mockMassTransitProducer = new Mock<IMassTransitProducer>();

            // Setup the Redis service to return the mock database
            _mockRedisService.Setup(r => r.GetDatabase(It.IsAny<int>())).Returns(_mockRedisDatabase.Object);
            _mockRedisService.Setup(r => r.ShadowDatabase).Returns(_mockRedisDatabase.Object);
            _mockRedisDatabase.Setup(db => db.CreateBatch(It.IsAny<object>())).Returns(_mockBatch.Object);
            _linkingService = new LinkingService(_mockAppSettings.Object, _mockRedisService.Object, _mockMassTransitProducer.Object);
        }

       
        [Fact]
        public async Task LinkingOnInsert_WhenMissingFP4_ShouldSkip()
        {
            // Arrange
            var fp5Json = JsonSerializer.Serialize(fp5MockData);
            var transactionJson = JsonSerializer.Serialize(transactionMockData);

            // Mock batch operations with missing FP4
            var fp4Task = Task.FromResult<RedisValue>(RedisValue.Null);
            var fp5Task = Task.FromResult<RedisValue>(fp5Json);
            var transactionTask = Task.FromResult<RedisValue>(transactionJson);

            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP4_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp4Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP5_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp5Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.TRANSACTION_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(transactionTask);

            // Act
            var result = await _linkingService.LinkingOnInsert("testSession");

            // Assert
            result.Should().BeFalse();
            _mockMassTransitProducer.Verify(m => m.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Never);
            _mockBatch.Verify(b => b.Execute(), Times.Once);
        }

        [Fact]
        public async Task LinkingOnInsert_WhenMissingFP5_ShouldSkip()
        {
            // Arrange
            var fp4Json = JsonSerializer.Serialize(fp4MockData);
            var transactionJson = JsonSerializer.Serialize(transactionMockData);

            // Mock batch operations with missing FP5
            var fp4Task = Task.FromResult<RedisValue>(fp4Json);
            var fp5Task = Task.FromResult<RedisValue>(RedisValue.Null);
            var transactionTask = Task.FromResult<RedisValue>(transactionJson);

            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP4_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp4Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP5_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp5Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.TRANSACTION_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(transactionTask);

            // Act
            var result = await _linkingService.LinkingOnInsert("testSession");

            // Assert
            result.Should().BeFalse();
            _mockMassTransitProducer.Verify(m => m.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Never);
            _mockBatch.Verify(b => b.Execute(), Times.Once);
        }

        [Fact]
        public async Task LinkingOnInsert_WhenMissingTransaction_ShouldSkip()
        {
            // Arrange
            var fp4Json = JsonSerializer.Serialize(fp4MockData);
            var fp5Json = JsonSerializer.Serialize(fp5MockData);

            // Mock batch operations with missing Transaction
            var fp4Task = Task.FromResult<RedisValue>(fp4Json);
            var fp5Task = Task.FromResult<RedisValue>(fp5Json);
            var transactionTask = Task.FromResult<RedisValue>(RedisValue.Null);

            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP4_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp4Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.FP5_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(fp5Task);
            _mockBatch.Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), _linkingService.TRANSACTION_REDIS_KEY, It.IsAny<CommandFlags>()))
                .Returns(transactionTask);

            // Act
            var result = await _linkingService.LinkingOnInsert("testSession");

            // Assert
            result.Should().BeFalse();
            _mockMassTransitProducer.Verify(m => m.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Never);
            _mockBatch.Verify(b => b.Execute(), Times.Once);
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

        [Fact]
        public void BuildDeviceModel_ShouldReturnCorrectModel()
        {
            // Arrange

            // Act
            var actual = _linkingService.BuildDeviceModel(transactionMockData);

            // Assert
            actual.Should().NotBeNull();
            Assert.Equal(transactionMockData.Di, actual.Di);
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
            actual.Device.Di.Should().BeEquivalentTo(transactionMockData.Di);
        }

    }
}
