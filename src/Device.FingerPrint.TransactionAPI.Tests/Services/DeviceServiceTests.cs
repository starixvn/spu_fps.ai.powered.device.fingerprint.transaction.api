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
        public void GetDeviceInfo_ShouldReturnDeviceInfo()
        {
            // Arrange
            var deviceInfo = "eyJuYSI6Ik4vQSIsImRldmljZUNvZGUiOiIwMDAzZTlkNzhjYmI5Mjg3ZjA3ZGJkMDc4OTQ0YzIyZDQ0ZGNmM2Q2OzgyNDM1ZjJhM2ExZWM4YWQwNmVjYTU3NGJhYzAzMTlkIiwiYXBwVmVyc2lvbiI6IjUuMCAoV2luZG93cyBOVCAxMC4wOyBXaW42NDsgeDY0KSBBcHBsZVdlYktpdC81MzcuMzYgKEtIVE1MLCBsaWtlIEdlY2tvKSBDaHJvbWUvMTQwLjAuMC4wIFNhZmFyaS81MzcuMzYiLCJ0aW1lWm9uZSI6Ii00MjAiLCJ1c2VyQWdlbnQiOiJNb3ppbGxhLzUuMCAoV2luZG93cyBOVCAxMC4wOyBXaW42NDsgeDY0KSBBcHBsZVdlYktpdC81MzcuMzYgKEtIVE1MLCBsaWtlIEdlY2tvKSBDaHJvbWUvMTQwLjAuMC4wIFNhZmFyaS81MzcuMzYiLCJzY3JlZW4iOnsid2lkdGgiOjE5MjAsImhlaWdodCI6MTA4MCwiY29sb3JEZXB0aCI6MjR9LCJkZXZpY2VJZCI6ImE0MGY1NmM2NThhNzQ4ZmZhNGI3ZGYzNWY5ZmIyNmM1IiwiaHJlZiI6Imh0dHBzOi8vYWxwOC5uZXhkZXYubmV0LyIsImNhcHR1cmVkRGF0ZSI6IjYzODk0MTc3MDYzMTIwMTEwMCIsImdlb2xvY2F0aW9uIjoiIiwidmVyc2lvbiI6IjIuMS41In0=";

            // Act
            var result = _deviceService.GetDeviceInfo(deviceInfo);

            // Assert
            Assert.NotNull(result);
        }
    }
}
