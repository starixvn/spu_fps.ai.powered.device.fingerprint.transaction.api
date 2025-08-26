using Device.FingerPrint.TransactionAPI.Queue;
using FluentAssertions;
using MassTransit;
using Moq;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Queue
{
    /// <summary>
    /// Tests for MassTransitProducer message queue operations
    /// </summary>
    public class MassTransitProducerTests
    {
        private readonly Mock<IBus> _mockBus;
        private readonly Mock<ISendEndpoint> _mockSendEndpoint;
        private readonly MassTransitProducer _massTransitProducer;
        private readonly string _testAddress = "rabbitmq://localhost/test-queue";

        public MassTransitProducerTests()
        {
            _mockBus = new Mock<IBus>();
            _mockSendEndpoint = new Mock<ISendEndpoint>();
            _massTransitProducer = new MassTransitProducer(_mockBus.Object);

            // Setup default successful endpoint resolution
            _mockBus.Setup(x => x.GetSendEndpoint(It.IsAny<Uri>()))
                .ReturnsAsync(_mockSendEndpoint.Object);
        }

        #region SendAsync Tests

        [Fact]
        public async Task SendAsync_WithValidData_ShouldCallSendEndpoint()
        {
            // Arrange
            var testData = new { Message = "Test message", Id = 123 };

            _mockSendEndpoint.Setup(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _massTransitProducer.SendAsync(testData, _testAddress);

            // Assert
            _mockBus.Verify(x => x.GetSendEndpoint(new Uri(_testAddress)), Times.Once);
            _mockSendEndpoint.Verify(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendAsync_WithStringData_ShouldHandleCorrectly()
        {
            // Arrange
            const string testMessage = "Simple string message";

            _mockSendEndpoint.Setup(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _massTransitProducer.SendAsync(testMessage, _testAddress);

            // Assert
            _mockSendEndpoint.Verify(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendAsync_WithComplexObject_ShouldSerializeAndSend()
        {
            // Arrange
            var complexData = new TestMessage
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Data = new Dictionary<string, object>
                {
                    { "key1", "value1" },
                    { "key2", 42 },
                    { "key3", true }
                },
                Tags = new[] { "tag1", "tag2", "tag3" }
            };

            _mockSendEndpoint.Setup(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _massTransitProducer.SendAsync(complexData, _testAddress);

            // Assert
            _mockSendEndpoint.Verify(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendAsync_WhenEndpointResolutionFails_ShouldThrowException()
        {
            // Arrange
            var testData = new { Message = "Test" };
            _mockBus.Setup(x => x.GetSendEndpoint(It.IsAny<Uri>()))
                .ThrowsAsync(new Exception("Endpoint resolution failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => 
                _massTransitProducer.SendAsync(testData, _testAddress));
        }

        [Fact]
        public async Task SendAsync_WhenSendFails_ShouldThrowException()
        {
            // Arrange
            var testData = new { Message = "Test" };
            _mockSendEndpoint.Setup(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Send operation failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => 
                _massTransitProducer.SendAsync(testData, _testAddress));
        }

        #endregion

        #region SendBatchAsync Tests

        [Fact]
        public async Task SendBatchAsync_WithMultipleItems_ShouldCallGetSendEndpoint()
        {
            // Arrange
            var batchData = new[]
            {
                new TestMessage { Id = Guid.NewGuid(), Data = new Dictionary<string, object> { { "item", 1 } } },
                new TestMessage { Id = Guid.NewGuid(), Data = new Dictionary<string, object> { { "item", 2 } } },
                new TestMessage { Id = Guid.NewGuid(), Data = new Dictionary<string, object> { { "item", 3 } } }
            };

            // Act
            await _massTransitProducer.SendBatchAsync(batchData, _testAddress);

            // Assert
            _mockBus.Verify(x => x.GetSendEndpoint(new Uri(_testAddress)), Times.Once);
        }

        [Fact]
        public async Task SendBatchAsync_WithEmptyCollection_ShouldStillCallGetSendEndpoint()
        {
            // Arrange
            var emptyBatch = Array.Empty<TestMessage>();

            // Act
            await _massTransitProducer.SendBatchAsync(emptyBatch, _testAddress);

            // Assert
            _mockBus.Verify(x => x.GetSendEndpoint(new Uri(_testAddress)), Times.Once);
        }

        [Fact]
        public async Task SendBatchAsync_WithLargeCollection_ShouldHandleCorrectly()
        {
            // Arrange
            var largeBatch = Enumerable.Range(1, 1000)
                .Select(i => new TestMessage 
                { 
                    Id = Guid.NewGuid(), 
                    Data = new Dictionary<string, object> { { "item", i } } 
                })
                .ToArray();

            // Act
            await _massTransitProducer.SendBatchAsync(largeBatch, _testAddress);

            // Assert
            _mockBus.Verify(x => x.GetSendEndpoint(new Uri(_testAddress)), Times.Once);
        }

        [Fact]
        public async Task SendBatchAsync_WhenEndpointResolutionFails_ShouldThrowException()
        {
            // Arrange
            var batchData = new[] { new TestMessage { Id = Guid.NewGuid() } };
            _mockBus.Setup(x => x.GetSendEndpoint(It.IsAny<Uri>()))
                .ThrowsAsync(new Exception("Endpoint resolution failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => 
                _massTransitProducer.SendBatchAsync(batchData, _testAddress));
        }

        #endregion

        #region Send (Fire and Forget) Tests

        [Fact]
        public void Send_WithValidData_ShouldInitiateAsyncOperation()
        {
            // Arrange
            var testData = new { Message = "Fire and forget test" };
            var taskCompletionSource = new TaskCompletionSource<ISendEndpoint>();
            taskCompletionSource.SetResult(_mockSendEndpoint.Object);

            _mockBus.Setup(x => x.GetSendEndpoint(It.IsAny<Uri>()))
                .Returns(taskCompletionSource.Task);

            _mockSendEndpoint.Setup(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            _massTransitProducer.Send(testData, _testAddress);

            // Wait a moment for the continuation to execute
            Thread.Sleep(100);

            // Assert
            _mockBus.Verify(x => x.GetSendEndpoint(new Uri(_testAddress)), Times.Once);
            // Note: We can't easily verify the Send call because it's in a continuation
        }

        [Fact]
        public void Send_WhenEndpointResolutionFails_ShouldNotThrowSynchronously()
        {
            // Arrange
            var testData = new { Message = "Test" };
            var taskCompletionSource = new TaskCompletionSource<ISendEndpoint>();
            taskCompletionSource.SetException(new Exception("Endpoint resolution failed"));

            _mockBus.Setup(x => x.GetSendEndpoint(It.IsAny<Uri>()))
                .Returns(taskCompletionSource.Task);

            // Act & Assert
            // Should not throw synchronously since it's fire-and-forget
            Action act = () => _massTransitProducer.Send(testData, _testAddress);
            act.Should().NotThrow();
        }

        [Fact]
        public void Send_WithNullData_ShouldStillAttemptToSend()
        {
            // Arrange
            object? nullData = null;
            var taskCompletionSource = new TaskCompletionSource<ISendEndpoint>();
            taskCompletionSource.SetResult(_mockSendEndpoint.Object);

            _mockBus.Setup(x => x.GetSendEndpoint(It.IsAny<Uri>()))
                .Returns(taskCompletionSource.Task);

            // Act
            _massTransitProducer.Send(nullData!, _testAddress);

            // Assert
            _mockBus.Verify(x => x.GetSendEndpoint(new Uri(_testAddress)), Times.Once);
        }

        #endregion

        #region Address Validation Tests

        [Theory]
        [InlineData("rabbitmq://localhost/queue-name")]
        [InlineData("amazonsqs://us-east-1/queue-name")]
        [InlineData("activemq://localhost:61616/queue-name")]
        public async Task SendAsync_WithDifferentAddressFormats_ShouldHandleCorrectly(string address)
        {
            // Arrange
            var testData = new { Message = "Test" };
            var expectedUri = new Uri(address);

            _mockBus.Setup(x => x.GetSendEndpoint(expectedUri))
                .ReturnsAsync(_mockSendEndpoint.Object);

            _mockSendEndpoint.Setup(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _massTransitProducer.SendAsync(testData, address);

            // Assert
            _mockBus.Verify(x => x.GetSendEndpoint(expectedUri), Times.Once);
        }

        [Theory]
        [InlineData("invalid-uri")]
        [InlineData("")]
        [InlineData("://no-scheme")]
        public async Task SendAsync_WithInvalidAddress_ShouldThrowException(string invalidAddress)
        {
            // Arrange
            var testData = new { Message = "Test" };

            // Act & Assert
            await Assert.ThrowsAsync<UriFormatException>(() => 
                _massTransitProducer.SendAsync(testData, invalidAddress));
        }

        [Theory]
        [InlineData("ftp://invalid-protocol/queue")]
        public async Task SendAsync_WithUnsupportedTransport_ShouldThrowException(string unsupportedAddress)
        {
            // Arrange
            var testData = new { Message = "Test" };
            _mockBus.Setup(x => x.GetSendEndpoint(It.IsAny<Uri>()))
                .ThrowsAsync(new ArgumentException("Unsupported transport scheme"));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _massTransitProducer.SendAsync(testData, unsupportedAddress));
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task SendAsync_HighThroughput_ShouldHandleMultipleConcurrentCalls()
        {
            // Arrange
            const int messageCount = 100;
            var testData = new { Message = "Performance test" };
            var tasks = new List<Task>();

            _mockSendEndpoint.Setup(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < messageCount; i++)
            {
                tasks.Add(_massTransitProducer.SendAsync(testData, _testAddress));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete in less than 5 seconds
            _mockBus.Verify(x => x.GetSendEndpoint(new Uri(_testAddress)), Times.Exactly(messageCount));
        }

        [Fact]
        public async Task SendBatchAsync_LargePayload_ShouldCompleteReasonablyQuickly()
        {
            // Arrange
            const int batchSize = 10000;
            var largeBatch = Enumerable.Range(1, batchSize)
                .Select(i => new TestMessage 
                { 
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    Data = new Dictionary<string, object> 
                    { 
                        { "index", i },
                        { "data", new string('x', 100) } // 100 character string per message
                    }
                })
                .ToArray();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            await _massTransitProducer.SendBatchAsync(largeBatch, _testAddress);

            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete in less than 1 second
            _mockBus.Verify(x => x.GetSendEndpoint(new Uri(_testAddress)), Times.Once);
        }

        #endregion

        #region Integration-style Tests

        [Fact]
        public async Task MixedOperations_SendAndBatch_ShouldWorkTogether()
        {
            // Arrange
            var singleMessage = new TestMessage { Id = Guid.NewGuid(), Data = new Dictionary<string, object> { { "type", "single" } } };
            var batchMessages = new[]
            {
                new TestMessage { Id = Guid.NewGuid(), Data = new Dictionary<string, object> { { "type", "batch1" } } },
                new TestMessage { Id = Guid.NewGuid(), Data = new Dictionary<string, object> { { "type", "batch2" } } }
            };

            // Setup for both generic and non-generic Send calls
            _mockSendEndpoint.Setup(x => x.Send(It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockSendEndpoint.Setup(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _massTransitProducer.SendAsync(singleMessage, _testAddress);
            await _massTransitProducer.SendBatchAsync(batchMessages, _testAddress);

            // Assert
            // Verify that GetSendEndpoint was called twice (once for SendAsync, once for SendBatchAsync)
            _mockBus.Verify(x => x.GetSendEndpoint(new Uri(_testAddress)), Times.Exactly(2));
            
            // We can verify the total number of Send calls (generic + non-generic)
            // SendAsync makes 1 call, SendBatchAsync makes 2 calls (one per item in batch)
            _mockSendEndpoint.Verify(
                x => x.Send(It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()), 
                Times.AtLeast(1));
        }

        #endregion

        #region Test Helper Classes

        public class TestMessage
        {
            public Guid Id { get; set; }
            public DateTime Timestamp { get; set; }
            public Dictionary<string, object> Data { get; set; } = new();
            public string[] Tags { get; set; } = Array.Empty<string>();
        }

        #endregion
    }
}