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
        public async Task ValidateAsync_ShouldReturnTrue()
        {
            // Arrange
            var subscriberName = "validSubscriber";

            // Act
            var result = await _subscriberService.ValidateAsync(subscriberName);

            // Assert
            result.Should().Be(-1);
        }

        [Fact]
        public async Task ValidateAsync_WithValidSubscriber_ShouldReturnSuccessAndSetSubscriberId()
        {
            // Arrange
            var subscriberName = "testSubscriber";
            var expectedSubscriberId = 123;
            var cacheKey = $"TransAPI:{subscriberName}";

            var subscriberModel = new SubscriberModel
            {
                SubscriberId = expectedSubscriberId,
                SubscriberName = subscriberName
            };

            _mockRedisService.Setup(x => x.GetAsync<SubscriberModel>(It.IsAny<IDatabase>(), cacheKey))
                .ReturnsAsync(subscriberModel);

            // Act
            var result = await _subscriberService.ValidateAsync(subscriberName);

            // Assert
            result.Should().NotBe(-1);
        }

        [Fact]
        public async Task ValidateAsync_WithWhitespaceSubscriber_ShouldReturnNotExistSubscriber()
        {
            // Arrange
            var subscriberName = "  ";

            // Act
            var result = await _subscriberService.ValidateAsync(subscriberName);

            // Assert
            result.Should().Be(-1);
        }

        [Fact]
        public async Task ValidateAsync_WithNullSubscriber_ShouldReturnNotExistSubscriber()
        {
            // Arrange
            string? subscriberName = null;


            // Act
            var result = await _subscriberService.ValidateAsync(subscriberName);

            // Assert
            result.Should().Be(-1);
        }

        #endregion

        #region Caching Tests

        [Fact]
        public async Task ValidateAsync_WithCacheHit_ShouldReturnSuccessAndNotCallApi()
        {
            // Arrange
            var subscriberName = "cachedSubscriber";
            var cacheKey = $"TransAPI:{subscriberName}";
            
            var cachedSubscriber = new SubscriberModel
            {
                SubscriberId = 456,
                SubscriberName = subscriberName
            };

            _mockRedisService.Setup(x => x.GetAsync<SubscriberModel>(It.IsAny<IDatabase>(), cacheKey))
                .ReturnsAsync(cachedSubscriber);

            // Act
            var result = await _subscriberService.ValidateAsync(subscriberName);

            // Assert
            result.Should().NotBe(-1);

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
            

            _mockRedisService.Setup(x => x.GetAsync<SubscriberModel>(It.IsAny<IDatabase>(), cacheKey))
                .ThrowsAsync(new Exception("Redis connection failed"));

            MockHttpCall(HttpStatusCode.OK, "{'SubscriberId': 500, 'SubscriberName': 'subscriberWithRedisError'}");

            // Act
            var result = await _subscriberService.ValidateAsync(subscriberName);

            // Assert
            result.Should().NotBe(-1);
        }

        [Fact]
        public async Task ValidateAsync_WithNullSubscriberFromCache_ShouldFallbackToHttpCall()
        {
            // Arrange
            var subscriberName = "fallbackSubscriber";
            var cacheKey = $"TransAPI:{subscriberName}";
            
            _mockRedisService.Setup(x => x.GetAsync<SubscriberModel>(It.IsAny<IDatabase>(), cacheKey))
                .ReturnsAsync((SubscriberModel?)null);

            MockHttpCall(HttpStatusCode.OK, "{'SubscriberId': 501, 'SubscriberName': 'fallbackSubscriber'}");

            // Act
            var result = await _subscriberService.ValidateAsync(subscriberName);

            // Assert
            result.Should().NotBe(-1);

            _mockRedisService.Verify(x => x.GetAsync<SubscriberModel>(It.IsAny<IDatabase>(), cacheKey), Times.Once);
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        #endregion
    }
}