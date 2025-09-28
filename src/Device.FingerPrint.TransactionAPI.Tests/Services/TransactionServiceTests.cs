using Device.FingerPrint.Models;
using Device.FingerPrint.TransactionAPI.Services;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Services
{
    public class TransactionServiceTests
    {
        private readonly Mock<IRedisMultiplexDbService> _mockRedisService;
        private readonly Mock<ILinkingService> _mockLinkingService;
        private readonly Mock<IAppSettings> _mockAppSettings;
        private readonly TransactionService _transactionService;
        private readonly Mock<IDatabase> _mockRedisDatabase;

        public TransactionServiceTests()
        {
            _mockRedisService = new Mock<IRedisMultiplexDbService>();
            _mockLinkingService = new Mock<ILinkingService>();
            _mockAppSettings = new Mock<IAppSettings>();
            _mockRedisDatabase = new Mock<IDatabase>();

            var redisSettings = new RedisSettings
            {
                TransactionTTLInSeconds = 90
            };
            _mockAppSettings.Setup(r => r.RedisSettings).Returns(redisSettings);

            _mockRedisService.Setup(r => r.ShadowDatabase).Returns(_mockRedisDatabase.Object);
            _mockRedisService.Setup(r => r.FP4Database).Returns(_mockRedisDatabase.Object);
            _mockRedisService.Setup(r => r.FP5Database).Returns(_mockRedisDatabase.Object);
            _mockRedisService.Setup(r => r.TransactionDatabase).Returns(_mockRedisDatabase.Object);

            _transactionService = new TransactionService(_mockAppSettings.Object, _mockRedisService.Object, _mockLinkingService.Object);
        }

        [Fact]
        public async Task InsertTransaction_ShouldNotInsertRedisAndUpdateExpireTime_WhenPushToQueue()
        {
            // Arrange
            var transaction = new TransactionModel();
            _mockLinkingService
                .Setup(s => s.LinkingOnInsert(transaction, true))
                .ReturnsAsync(true);

            // Act
            await _transactionService.InsertTransaction(transaction);

            // Assert
            _mockRedisService.Verify(r => r.TransactionDatabase.SortedSetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), It.IsAny<CommandFlags>()), Times.Never);
            _mockRedisService.Verify(r => r.ShadowDatabase.SortedSetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), It.IsAny<CommandFlags>()), Times.Never);
        }

        [Fact]
        public async Task InsertTransaction_ShouldInsertRedisAndUpdateExpireTime_WhenMissingFingerprint()
        {
            // Arrange
            var transaction = new TransactionModel
            {
                SessionKey = "session",
                TransDate = DateTimeOffset.Now.DateTime
            };

            _mockLinkingService
                .Setup(s => s.LinkingOnInsert(transaction, true))
                .ReturnsAsync(false);

            // Act
            await _transactionService.InsertTransaction(transaction);

            // Assert
            _mockRedisService.Verify(r => r.ShadowDatabase.SortedSetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), It.IsAny<CommandFlags>()), Times.Never);
        }
    }
}
