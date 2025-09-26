using Device.FingerPrint.Model;
using Device.FingerPrint.TransactionAPI.Services;
using FluentAssertions;
using Moq;
using Moq.Protected;
using StackExchange.Redis;
using System.Diagnostics;
using System.Net;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Services
{
    /// <summary>
    /// Tests for SubscriberService which validates transaction requests and manages subscriber caching
    /// </summary>
    public class SubscriberServiceTests
    {
        private readonly Mock<IAppSettings> _mockAppSettings;
        private readonly Mock<IRedisMultiplexDbService> _mockRedisService;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly SubscriberService _subscriberService;

        public SubscriberServiceTests()
        {
            _mockAppSettings = new Mock<IAppSettings>();
            _mockRedisService = new Mock<IRedisMultiplexDbService>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            var mockLogger = new Mock<ILogger>();
            _subscriberService = new SubscriberService(_mockAppSettings.Object, _mockRedisService.Object, httpClient, mockLogger.Object);

            // Setup default app settings
            _mockAppSettings.Setup(x => x.GetSubscriberInfo).Returns("https://api.example.com/subscriber");
        }

        private void MockHttpCall(HttpStatusCode statusCode, string responseContent)
        {
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseContent),
                });
        }

        #region ValidateAsync Tests

        [Fact]
        public async Task ValidateAsync_WithEmptyAccount_ShouldReturnEmptyAccount()
        {
            // Arrange
            var transactionRequest = new TransactionRequest
            {
                Account = string.Empty,
                Subscriber = "validSubscriber"
            };

            // Act
            var result = await _subscriberService.ValidateAsync(transactionRequest);

            // Assert
            result.Should().Be(TransactionResult.EmptyAccount);
        }

        [Fact]
        public async Task ValidateAsync_WithNullAccount_ShouldReturnEmptyAccount()
        {
            // Arrange
            var transactionRequest = new TransactionRequest
            {
                Account = null,
                Subscriber = "validSubscriber"
            };

            // Act
            var result = await _subscriberService.ValidateAsync(transactionRequest);

            // Assert
            result.Should().Be(TransactionResult.EmptyAccount);
        }

        [Fact]
        public async Task ValidateAsync_WithValidSubscriber_ShouldReturnSuccessAndSetSubscriberId()
        {
            // Arrange
            var subscriberName = "testSubscriber";
            var expectedSubscriberId = 123;
            var cacheKey = $"TransAPI:{subscriberName}";
            
            var transactionRequest = new TransactionRequest
            {
                Account = "testAccount",
                Subscriber = subscriberName
            };

            var subscriberModel = new SubscriberModel
            {
                SubscriberId = expectedSubscriberId,
                SubscriberName = subscriberName
            };

            _mockRedisService.Setup(x => x.GetAsync<SubscriberModel>(It.IsAny<IDatabase>(), cacheKey))
                .ReturnsAsync(subscriberModel);

            // Act
            var result = await _subscriberService.ValidateAsync(transactionRequest);

            // Assert
            result.Should().Be(TransactionResult.Success);
            transactionRequest.SubscriberId.Should().Be(expectedSubscriberId);
        }

        [Fact]
        public async Task ValidateAsync_WithEmptySubscriber_ShouldReturnNotExistSubscriber()
        {
            // Arrange
            var transactionRequest = new TransactionRequest
            {
                Account = "testAccount",
                Subscriber = string.Empty
            };

            // Act
            var result = await _subscriberService.ValidateAsync(transactionRequest);

            // Assert
            result.Should().Be(TransactionResult.NotExistSubscriber);
        }

        [Fact]
        public async Task ValidateAsync_WithWhitespaceSubscriber_ShouldReturnNotExistSubscriber()
        {
            // Arrange
            var transactionRequest = new TransactionRequest
            {
                Account = "testAccount",
                Subscriber = "   "
            };

            // Act
            var result = await _subscriberService.ValidateAsync(transactionRequest);

            // Assert
            result.Should().Be(TransactionResult.NotExistSubscriber);
        }

        [Fact]
        public async Task ValidateAsync_WithNullSubscriber_ShouldReturnNotExistSubscriber()
        {
            // Arrange
            var transactionRequest = new TransactionRequest
            {
                Account = "testAccount",
                Subscriber = null
            };

            // Act
            var result = await _subscriberService.ValidateAsync(transactionRequest);

            // Assert
            result.Should().Be(TransactionResult.NotExistSubscriber);
        }

        #endregion

        #region Caching Tests

        [Fact]
        public async Task ValidateAsync_WithCacheHit_ShouldReturnSuccessAndNotCallApi()
        {
            // Arrange
            var subscriberName = "cachedSubscriber";
            var cacheKey = $"TransAPI:{subscriberName}";
            
            var transactionRequest = new TransactionRequest
            {
                Account = "testAccount",
                Subscriber = subscriberName
            };

            var cachedSubscriber = new SubscriberModel
            {
                SubscriberId = 456,
                SubscriberName = subscriberName
            };

            _mockRedisService.Setup(x => x.GetAsync<SubscriberModel>(It.IsAny<IDatabase>(), cacheKey))
                .ReturnsAsync(cachedSubscriber);

            // Act
            var result = await _subscriberService.ValidateAsync(transactionRequest);

            // Assert
            result.Should().Be(TransactionResult.Success);
            transactionRequest.SubscriberId.Should().Be(456);

            _mockRedisService.Verify(x => x.GetAsync<SubscriberModel>(It.IsAny<IDatabase>(), cacheKey), Times.Once);
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task ValidateAsync_WithRedisException_ShouldFallbackToApiAndSucceed()
        {
            // Arrange
            var subscriberName = "subscriberWithRedisError";
            var cacheKey = $"TransAPI:{subscriberName}";
            
            var transactionRequest = new TransactionRequest
            {
                Account = "testAccount",
                Subscriber = subscriberName
            };

            _mockRedisService.Setup(x => x.GetAsync<SubscriberModel>(It.IsAny<IDatabase>(), cacheKey))
                .ThrowsAsync(new Exception("Redis connection failed"));

            MockHttpCall(HttpStatusCode.OK, "{'SubscriberId': 500, 'SubscriberName': 'subscriberWithRedisError'}");

            // Act
            var result = await _subscriberService.ValidateAsync(transactionRequest);

            // Assert
            result.Should().Be(TransactionResult.Success);
            transactionRequest.SubscriberId.Should().Be(500);
        }

        [Fact]
        public async Task ValidateAsync_WithZeroSubscriberId_ShouldReturnNotExistSubscriber()
        {
            // Arrange
            var subscriberName = "zeroIdSubscriber";
            var cacheKey = $"TransAPI:{subscriberName}";
            
            var transactionRequest = new TransactionRequest
            {
                Account = "testAccount",
                Subscriber = subscriberName
            };

            var subscriberModel = new SubscriberModel
            {
                SubscriberId = 0, // Invalid subscriber ID
                SubscriberName = subscriberName
            };

            _mockRedisService.Setup(x => x.GetAsync<SubscriberModel>(It.IsAny<IDatabase>(), cacheKey))
                .ReturnsAsync(subscriberModel);

            // Act
            var result = await _subscriberService.ValidateAsync(transactionRequest);

            // Assert
            result.Should().Be(TransactionResult.NotExistSubscriber);
        }

        [Fact]
        public async Task ValidateAsync_WithNullSubscriberFromCache_ShouldFallbackToHttpCall()
        {
            // Arrange
            var subscriberName = "fallbackSubscriber";
            var cacheKey = $"TransAPI:{subscriberName}";
            
            var transactionRequest = new TransactionRequest
            {
                Account = "testAccount",
                Subscriber = subscriberName
            };

            _mockRedisService.Setup(x => x.GetAsync<SubscriberModel>(It.IsAny<IDatabase>(), cacheKey))
                .ReturnsAsync((SubscriberModel?)null);

            MockHttpCall(HttpStatusCode.OK, "{'SubscriberId': 501, 'SubscriberName': 'fallbackSubscriber'}");

            // Act
            var result = await _subscriberService.ValidateAsync(transactionRequest);

            // Assert
            result.Should().Be(TransactionResult.Success);
            transactionRequest.SubscriberId.Should().Be(501);

            _mockRedisService.Verify(x => x.GetAsync<SubscriberModel>(It.IsAny<IDatabase>(), cacheKey), Times.Once);
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task ValidateAsync_ConcurrentCalls_ShouldHandleCorrectly()
        {
            // Arrange
            const int concurrentCalls = 50; // Increased for a more robust test
            var subscriberName = "concurrentSubscriber";
            var cacheKey = $"TransAPI:{subscriberName}";

            var subscriberModel = new SubscriberModel
            {
                SubscriberId = 777,
                SubscriberName = subscriberName
            };

            _mockRedisService.Setup(x => x.GetAsync<SubscriberModel>(It.IsAny<IDatabase>(), cacheKey))
                .ReturnsAsync(subscriberModel);

            var tasks = new List<Task>();

            // Act
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < concurrentCalls; i++)
            {
                var request = new TransactionRequest
                {
                    Account = $"account{i}",
                    Subscriber = subscriberName
                };
                tasks.Add(Task.Run(() => _subscriberService.ValidateAsync(request)));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            // All tasks should complete successfully and quickly
            tasks.ForEach(t => ((Task<string>)t).Result.Should().Be(TransactionResult.Success));
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(500); // Should be very fast
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task ValidateAsync_WithLongSubscriberName_ShouldHandleCorrectly()
        {
            // Arrange
            var longSubscriberName = new string('a', 1000);
            var cacheKey = $"TransAPI:{longSubscriberName}";
            
            var transactionRequest = new TransactionRequest
            {
                Account = "testAccount",
                Subscriber = longSubscriberName
            };

            var subscriberModel = new SubscriberModel
            {
                SubscriberId = 999,
                SubscriberName = longSubscriberName
            };

            _mockRedisService.Setup(x => x.GetAsync<SubscriberModel>(It.IsAny<IDatabase>(), cacheKey))
                .ReturnsAsync(subscriberModel);

            // Act
            var result = await _subscriberService.ValidateAsync(transactionRequest);

            // Assert
            result.Should().Be(TransactionResult.Success);
            transactionRequest.SubscriberId.Should().Be(999);
        }

        [Fact]
        public async Task ValidateAsync_WithSpecialCharactersInSubscriberName_ShouldHandleCorrectly()
        {
            // Arrange
            var specialCharSubscriber = "test@#$%^&*()_+{}|:<>?";
            var cacheKey = $"TransAPI:{specialCharSubscriber}";
            
            var transactionRequest = new TransactionRequest
            {
                Account = "testAccount",
                Subscriber = specialCharSubscriber
            };

            var subscriberModel = new SubscriberModel
            {
                SubscriberId = 888,
                SubscriberName = specialCharSubscriber
            };

            _mockRedisService.Setup(x => x.GetAsync<SubscriberModel>(It.IsAny<IDatabase>(), cacheKey))
                .ReturnsAsync(subscriberModel);

            // Act
            var result = await _subscriberService.ValidateAsync(transactionRequest);

            // Assert
            result.Should().Be(TransactionResult.Success);
            transactionRequest.SubscriberId.Should().Be(888);
        }

        #endregion
    }
}