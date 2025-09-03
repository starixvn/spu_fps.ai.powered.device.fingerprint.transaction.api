using Device.FingerPrint.TransactionAPI.Model;
using Device.FingerPrint.TransactionAPI.Queue;
using Device.FingerPrint.TransactionAPI.Services;
using Fanex.Logging;
using FluentAssertions;
using Moq;
using System.Globalization;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Services
{
    public class LinkingServiceTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IAppSettings> _mockAppSettings;
        private readonly Mock<IRedisService> _mockRedisService;
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
            Graphics = "GraphicsData",
        };

        private readonly TransactionRequest transactionMockData = new TransactionRequest()
        {
            Account = "testAccount",
            Di = "testDi",
            __di = "test__di",
        };

        public LinkingServiceTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockAppSettings = new Mock<IAppSettings>();
            _mockRedisService = new Mock<IRedisService>();
            _mockMassTransitProducer = new Mock<IMassTransitProducer>();
            _linkingService = new LinkingService(_mockLogger.Object, _mockAppSettings.Object, _mockRedisService.Object, _mockMassTransitProducer.Object);
        }

        [Fact]
        public async Task LinkingOnInsert_WhenHasThreeDataTypes_ShouldPublishMessageToQueue()
        {
            // Arrange
            _mockRedisService
                .Setup(r => r.HashExistAsync<string>(It.IsAny<string>(), _linkingService.FP4_REDIS_KEY))
                .ReturnsAsync(true);

            _mockRedisService
                .Setup(r => r.HashGetAsync<APFingerprintModel>(It.IsAny<string>(), _linkingService.FP4_REDIS_KEY))
                .ReturnsAsync(fp4MockData);

            _mockRedisService
                .Setup(r => r.HashExistAsync<string>(It.IsAny<string>(), _linkingService.FP5_REDIS_KEY))
                .ReturnsAsync(true);

            _mockRedisService
                .Setup(r => r.HashGetAsync<APFingerprintModel>(It.IsAny<string>(), _linkingService.FP5_REDIS_KEY))
                .ReturnsAsync(fp5MockData);

            _mockRedisService
                .Setup(r => r.HashExistAsync<string>(It.IsAny<string>(), _linkingService.TRANSACTION_REDIS_KEY))
                .ReturnsAsync(true);

            _mockRedisService
               .Setup(r => r.HashGetAsync<TransactionRequest>(It.IsAny<string>(), _linkingService.TRANSACTION_REDIS_KEY))
               .ReturnsAsync(transactionMockData);

            var expectedMessage = new MessageQueue
            {
                Account = transactionMockData.Account,
                Device = new DeviceModel
                {
                    AcceptsTypes = transactionMockData.AcceptsTypes,
                    DetecasAnalysis = transactionMockData.DetecasAnalysis,
                    Di = transactionMockData.Di ?? transactionMockData.__di,
                    Ip = transactionMockData.Ip,
                    Url = transactionMockData.Url,
                    Platform = transactionMockData.Platform,
                    Screen = transactionMockData.Screen,
                    TotalMinutes = (DateTimeOffset.UtcNow - DateTimeOffset.Now).TotalMinutes.ToString(CultureInfo.InvariantCulture),
                    UserAgent = transactionMockData.UserAgent
                },
                Action = transactionMockData.Action,
                ActionResult = transactionMockData.ActionResult,
                Subscriber = transactionMockData.Subscriber,
                SubscriberId = transactionMockData.SubscriberId,
                IP = transactionMockData.Ip,
                FakeIP = transactionMockData.FakeIp,
                TransDate = transactionMockData.TransDate,
                SubscriberDate = DateTimeOffset.Now,
                Token = transactionMockData.Token,
                Recaptcha = "",
                SessionKey = transactionMockData.SessionKey,
                BotD = transactionMockData.Bi,
                FingerPrint = new APFingerprintModel
                {
                    Audio = fp4MockData.Audio,
                    Browser = fp4MockData.Browser,
                    Graphics = fp5MockData.Graphics,
                    Hardware = fp4MockData.Hardware,
                    Preferences = fp4MockData.Preferences
                }
            };

            // Act
            await _linkingService.LinkingOnInsert("testSession");

            // Assert
            _mockMassTransitProducer.Verify(m => m.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task LinkingOnInsert_WhenMissingFP4_ShouldSkip()
        {
            // Arrange
            _mockRedisService
                .Setup(r => r.HashExistAsync<string>(It.IsAny<string>(), _linkingService.FP5_REDIS_KEY))
                .ReturnsAsync(true);

            _mockRedisService
                .Setup(r => r.HashGetAsync<APFingerprintModel>(It.IsAny<string>(), _linkingService.FP5_REDIS_KEY))
                .ReturnsAsync(fp5MockData);

            _mockRedisService
                .Setup(r => r.HashExistAsync<string>(It.IsAny<string>(), _linkingService.TRANSACTION_REDIS_KEY))
                .ReturnsAsync(true);

            _mockRedisService
               .Setup(r => r.HashGetAsync<TransactionRequest>(It.IsAny<string>(), _linkingService.TRANSACTION_REDIS_KEY))
               .ReturnsAsync(transactionMockData);

            var expectedMessage = new MessageQueue
            {
                Account = transactionMockData.Account,
                Device = new DeviceModel
                {
                    AcceptsTypes = transactionMockData.AcceptsTypes,
                    DetecasAnalysis = transactionMockData.DetecasAnalysis,
                    Di = transactionMockData.Di ?? transactionMockData.__di,
                    Ip = transactionMockData.Ip,
                    Url = transactionMockData.Url,
                    Platform = transactionMockData.Platform,
                    Screen = transactionMockData.Screen,
                    TotalMinutes = (DateTimeOffset.UtcNow - DateTimeOffset.Now).TotalMinutes.ToString(CultureInfo.InvariantCulture),
                    UserAgent = transactionMockData.UserAgent
                },
                Action = transactionMockData.Action,
                ActionResult = transactionMockData.ActionResult,
                Subscriber = transactionMockData.Subscriber,
                SubscriberId = transactionMockData.SubscriberId,
                IP = transactionMockData.Ip,
                FakeIP = transactionMockData.FakeIp,
                TransDate = transactionMockData.TransDate,
                SubscriberDate = DateTimeOffset.Now,
                Token = transactionMockData.Token,
                Recaptcha = "",
                SessionKey = transactionMockData.SessionKey,
                BotD = transactionMockData.Bi,
                FingerPrint = new APFingerprintModel
                {
                    Audio = fp4MockData.Audio,
                    Browser = fp4MockData.Browser,
                    Graphics = fp5MockData.Graphics,
                    Hardware = fp4MockData.Hardware,
                    Preferences = fp4MockData.Preferences
                }
            };

            // Act
            await _linkingService.LinkingOnInsert("testSession");

            // Assert
            _mockMassTransitProducer.Verify(m => m.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LinkingOnInsert_WhenMissingFP5_ShouldSkip()
        {
            // Arrange
            _mockRedisService
                .Setup(r => r.HashExistAsync<string>(It.IsAny<string>(), _linkingService.FP4_REDIS_KEY))
                .ReturnsAsync(true);

            _mockRedisService
                .Setup(r => r.HashGetAsync<APFingerprintModel>(It.IsAny<string>(), _linkingService.FP4_REDIS_KEY))
                .ReturnsAsync(fp4MockData);

            _mockRedisService
                .Setup(r => r.HashExistAsync<string>(It.IsAny<string>(), _linkingService.TRANSACTION_REDIS_KEY))
                .ReturnsAsync(true);

            _mockRedisService
               .Setup(r => r.HashGetAsync<TransactionRequest>(It.IsAny<string>(), _linkingService.TRANSACTION_REDIS_KEY))
               .ReturnsAsync(transactionMockData);

            var expectedMessage = new MessageQueue
            {
                Account = transactionMockData.Account,
                Device = new DeviceModel
                {
                    AcceptsTypes = transactionMockData.AcceptsTypes,
                    DetecasAnalysis = transactionMockData.DetecasAnalysis,
                    Di = transactionMockData.Di ?? transactionMockData.__di,
                    Ip = transactionMockData.Ip,
                    Url = transactionMockData.Url,
                    Platform = transactionMockData.Platform,
                    Screen = transactionMockData.Screen,
                    TotalMinutes = (DateTimeOffset.UtcNow - DateTimeOffset.Now).TotalMinutes.ToString(CultureInfo.InvariantCulture),
                    UserAgent = transactionMockData.UserAgent
                },
                Action = transactionMockData.Action,
                ActionResult = transactionMockData.ActionResult,
                Subscriber = transactionMockData.Subscriber,
                SubscriberId = transactionMockData.SubscriberId,
                IP = transactionMockData.Ip,
                FakeIP = transactionMockData.FakeIp,
                TransDate = transactionMockData.TransDate,
                SubscriberDate = DateTimeOffset.Now,
                Token = transactionMockData.Token,
                Recaptcha = "",
                SessionKey = transactionMockData.SessionKey,
                BotD = transactionMockData.Bi,
                FingerPrint = new APFingerprintModel
                {
                    Audio = fp4MockData.Audio,
                    Browser = fp4MockData.Browser,
                    Graphics = fp5MockData.Graphics,
                    Hardware = fp4MockData.Hardware,
                    Preferences = fp4MockData.Preferences
                }
            };

            // Act
            await _linkingService.LinkingOnInsert("testSession");

            // Assert
            _mockMassTransitProducer.Verify(m => m.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LinkingOnInsert_WhenMissingTransaction_ShouldSkip()
        {
            // Arrange
            _mockRedisService
                .Setup(r => r.HashExistAsync<string>(It.IsAny<string>(), _linkingService.FP4_REDIS_KEY))
                .ReturnsAsync(true);

            _mockRedisService
                .Setup(r => r.HashGetAsync<APFingerprintModel>(It.IsAny<string>(), _linkingService.FP4_REDIS_KEY))
                .ReturnsAsync(fp4MockData);

            _mockRedisService
                .Setup(r => r.HashExistAsync<string>(It.IsAny<string>(), _linkingService.FP5_REDIS_KEY))
                .ReturnsAsync(true);

            _mockRedisService
                .Setup(r => r.HashGetAsync<APFingerprintModel>(It.IsAny<string>(), _linkingService.FP5_REDIS_KEY))
                .ReturnsAsync(fp5MockData);

            var expectedMessage = new MessageQueue
            {
                Account = transactionMockData.Account,
                Device = new DeviceModel
                {
                    AcceptsTypes = transactionMockData.AcceptsTypes,
                    DetecasAnalysis = transactionMockData.DetecasAnalysis,
                    Di = transactionMockData.Di ?? transactionMockData.__di,
                    Ip = transactionMockData.Ip,
                    Url = transactionMockData.Url,
                    Platform = transactionMockData.Platform,
                    Screen = transactionMockData.Screen,
                    TotalMinutes = (DateTimeOffset.UtcNow - DateTimeOffset.Now).TotalMinutes.ToString(CultureInfo.InvariantCulture),
                    UserAgent = transactionMockData.UserAgent
                },
                Action = transactionMockData.Action,
                ActionResult = transactionMockData.ActionResult,
                Subscriber = transactionMockData.Subscriber,
                SubscriberId = transactionMockData.SubscriberId,
                IP = transactionMockData.Ip,
                FakeIP = transactionMockData.FakeIp,
                TransDate = transactionMockData.TransDate,
                SubscriberDate = DateTimeOffset.Now,
                Token = transactionMockData.Token,
                Recaptcha = "",
                SessionKey = transactionMockData.SessionKey,
                BotD = transactionMockData.Bi,
                FingerPrint = new APFingerprintModel
                {
                    Audio = fp4MockData.Audio,
                    Browser = fp4MockData.Browser,
                    Graphics = fp5MockData.Graphics,
                    Hardware = fp4MockData.Hardware,
                    Preferences = fp4MockData.Preferences
                }
            };

            // Act
            await _linkingService.LinkingOnInsert("testSession");

            // Assert
            _mockMassTransitProducer.Verify(m => m.SendAsync(It.IsAny<MessageQueue>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void LinkFingerprintsAttributes_ShouldReturnCorrectModel()
        {
            // Arrange
            var expected = new APFingerprintModel
            {
                Audio = fp4MockData.Audio,
                Browser = fp4MockData.Browser,
                Graphics = fp5MockData.Graphics,
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
                Graphics = fp5MockData.Graphics,
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
                Graphics = fp5MockData.Graphics,
                Hardware = fp4MockData.Hardware,
                Preferences = fp4MockData.Preferences
            };

            // Act
            var actual = _linkingService.BuildMessageQueue(transactionMockData, fp4MockData, fp5MockData);

            // Assert
            actual.Should().NotBeNull();
            actual.FingerPrint.Should().BeEquivalentTo(expected);
            actual.Account.Should().BeEquivalentTo(transactionMockData.Account);
            actual.Device.Di.Should().BeEquivalentTo(transactionMockData.Di);
        }
    }
}
