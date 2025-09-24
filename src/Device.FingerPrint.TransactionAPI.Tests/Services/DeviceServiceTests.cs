using Device.FingerPrint.TransactionAPI.Services;
using Moq;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Services
{
    public class DeviceServiceTests
    {
        private readonly Mock<ILogger> _loggerMock;
        private readonly DeviceService _deviceService;
        public DeviceServiceTests()
        {
            _loggerMock = new Mock<ILogger>();
            _deviceService = new DeviceService(_loggerMock.Object);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("null")]
        public void GetDeviceInfo_ShouldReturnEmptyDeviceInfo_WhenDeviceCodeIsNullOrWhitespace(string deviceCode)
        {
            // Arrange
            
            // Act
            var result = _deviceService.GetDeviceInfo(deviceCode);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetDeviceInfo_ShouldReturnDeviceInfo_WhenIsOldFlow()
        {
            // Arrange
            var deviceInfo = "eyJuYSI6Ik4vQSIsImRldmljZUNvZGUiOiIwMDAzZTlkNzhjYmI5Mjg3ZjA3ZGJkMDc4OTQ0YzIyZDQ0ZGNmM2Q2OzgyNDM1ZjJhM2ExZWM4YWQwNmVjYTU3NGJhYzAzMTlkIiwiYXBwVmVyc2lvbiI6IjUuMCAoV2luZG93cyBOVCAxMC4wOyBXaW42NDsgeDY0KSBBcHBsZVdlYktpdC81MzcuMzYgKEtIVE1MLCBsaWtlIEdlY2tvKSBDaHJvbWUvMTQwLjAuMC4wIFNhZmFyaS81MzcuMzYiLCJ0aW1lWm9uZSI6Ii00MjAiLCJ1c2VyQWdlbnQiOiJNb3ppbGxhLzUuMCAoV2luZG93cyBOVCAxMC4wOyBXaW42NDsgeDY0KSBBcHBsZVdlYktpdC81MzcuMzYgKEtIVE1MLCBsaWtlIEdlY2tvKSBDaHJvbWUvMTQwLjAuMC4wIFNhZmFyaS81MzcuMzYiLCJzY3JlZW4iOnsid2lkdGgiOjE5MjAsImhlaWdodCI6MTA4MCwiY29sb3JEZXB0aCI6MjR9LCJkZXZpY2VJZCI6ImE0MGY1NmM2NThhNzQ4ZmZhNGI3ZGYzNWY5ZmIyNmM1IiwiaHJlZiI6Imh0dHBzOi8vYWxwOC5uZXhkZXYubmV0LyIsImNhcHR1cmVkRGF0ZSI6IjYzODk0MTc3MDYzMTIwMTEwMCIsImdlb2xvY2F0aW9uIjoiIiwidmVyc2lvbiI6IjIuMS41In0=";

            // Act
            var result = _deviceService.GetDeviceInfo(deviceInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.F4);
            Assert.Null(result.F5);
            Assert.Null(result.Rtc);
        }

        [Fact]
        public void GetDeviceInfo_ShouldReturnDeviceInfo_WhenIsNewFlow()
        {
            // Arrange
            var deviceInfo = "P3Rzn3qlq/D7hQqg1fa/BGYS1eITCz7TYe17hzME1/jjCzV5hSRrqT4TcTqlq/YLYK54qT4T1/6TBTRUCzM5YeYHC/I/he7UBGVo1Qa/1eh3hSYdh/V3CzYdYKqJq2Ij13qlqrVEYGhHCrYSCejjCz651r1jCGqo1GhzCQhg1rq3CrYHqT4T1/MTBTRThehzCf6SY/u41rq51Q1j1GD/Y/ajhzVd1QVghzjjBKqJq2REh3qlq/JdYeULBfqLYeY4y/q4BKRk";

            // Act
            var result = _deviceService.GetDeviceInfo(deviceInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.F4);
            Assert.NotNull(result.F5);
            Assert.NotNull(result.Rtc);
        }
    }
}
