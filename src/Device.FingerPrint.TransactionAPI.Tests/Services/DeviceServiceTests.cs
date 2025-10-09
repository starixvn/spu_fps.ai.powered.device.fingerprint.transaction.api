using Device.FingerPrint.TransactionAPI.Services;
using Moq;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Services
{
    public class DeviceServiceTests
    {
        private readonly DeviceService _deviceService;
        public DeviceServiceTests()
        {
            _deviceService = new DeviceService();
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
            Assert.Equal("-1", result.Version);
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
            Assert.NotEqual("-1", result.Version);
        }

        [Fact]
        public void GetDeviceInfo_ShouldReturnNullVersion_WhenVersionNotExists()
        {
            // Arrange
            var deviceInfo = "eyJuYSI6Ik4vQSIsImRldmljZUNvZGUiOiIwMDAzZTlkNzhjYmI5Mjg3ZjA3ZGJkMDc4OTQ0YzIyZDQ0ZGNmM2Q2OzgyNDM1ZjJhM2ExZWM4YWQwNmVjYTU3NGJhYzAzMTlkIiwiYXBwVmVyc2lvbiI6IjUuMCAoV2luZG93cyBOVCAxMC4wOyBXaW42NDsgeDY0KSBBcHBsZVdlYktpdC81MzcuMzYgKEtIVE1MLCBsaWtlIEdlY2tvKSBDaHJvbWUvMTQwLjAuMC4wIFNhZmFyaS81MzcuMzYiLCJ0aW1lWm9uZSI6Ii00MjAiLCJ1c2VyQWdlbnQiOiJNb3ppbGxhLzUuMCAoV2luZG93cyBOVCAxMC4wOyBXaW42NDsgeDY0KSBBcHBsZVdlYktpdC81MzcuMzYgKEtIVE1MLCBsaWtlIEdlY2tvKSBDaHJvbWUvMTQwLjAuMC4wIFNhZmFyaS81MzcuMzYiLCJzY3JlZW4iOnsid2lkdGgiOjE5MjAsImhlaWdodCI6MTA4MCwiY29sb3JEZXB0aCI6MjR9LCJkZXZpY2VJZCI6ImE0MGY1NmM2NThhNzQ4ZmZhNGI3ZGYzNWY5ZmIyNmM1IiwiaHJlZiI6Imh0dHBzOi8vYWxwOC5uZXhkZXYubmV0LyIsImNhcHR1cmVkRGF0ZSI6IjYzODk0MTc3MDYzMTIwMTEwMCIsImdlb2xvY2F0aW9uIjoiIn0=";

            // Act
            var result = _deviceService.GetDeviceInfo(deviceInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.F4);
            Assert.Null(result.F5);
            Assert.Null(result.Rtc);
            Assert.Equal("-1", result.Version);
        }

        [Fact]
        public void GetDeviceInfo_ShouldReturnEmptyVersion_WhenVersionEmpty()
        {
            // Arrange
            var deviceInfo = "eyJuYSI6Ik4vQSIsImRldmljZUNvZGUiOiIwMDAzZTlkNzhjYmI5Mjg3ZjA3ZGJkMDc4OTQ0YzIyZDQ0ZGNmM2Q2OzgyNDM1ZjJhM2ExZWM4YWQwNmVjYTU3NGJhYzAzMTlkIiwiYXBwVmVyc2lvbiI6IjUuMCAoV2luZG93cyBOVCAxMC4wOyBXaW42NDsgeDY0KSBBcHBsZVdlYktpdC81MzcuMzYgKEtIVE1MLCBsaWtlIEdlY2tvKSBDaHJvbWUvMTQwLjAuMC4wIFNhZmFyaS81MzcuMzYiLCJ0aW1lWm9uZSI6Ii00MjAiLCJ1c2VyQWdlbnQiOiJNb3ppbGxhLzUuMCAoV2luZG93cyBOVCAxMC4wOyBXaW42NDsgeDY0KSBBcHBsZVdlYktpdC81MzcuMzYgKEtIVE1MLCBsaWtlIEdlY2tvKSBDaHJvbWUvMTQwLjAuMC4wIFNhZmFyaS81MzcuMzYiLCJzY3JlZW4iOnsid2lkdGgiOjE5MjAsImhlaWdodCI6MTA4MCwiY29sb3JEZXB0aCI6MjR9LCJkZXZpY2VJZCI6ImE0MGY1NmM2NThhNzQ4ZmZhNGI3ZGYzNWY5ZmIyNmM1IiwiaHJlZiI6Imh0dHBzOi8vYWxwOC5uZXhkZXYubmV0LyIsImNhcHR1cmVkRGF0ZSI6IjYzODk0MTc3MDYzMTIwMTEwMCIsImdlb2xvY2F0aW9uIjoiIiwgInZlcnNpb24iOiIifQ==";

            // Act
            var result = _deviceService.GetDeviceInfo(deviceInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.F4);
            Assert.Null(result.F5);
            Assert.Null(result.Rtc);
            Assert.Equal("-2", result.Version);
        }

        [Fact]
        public void GetDeviceInfo_ShouldReturnDeviceInfo_WhenIsNewFlow()
        {
            // Arrange
            var deviceInfo = "P3Rzn3qlq/V4h/VgBeUHCzFU1/I/Y/U5Y/crBeMz1/I7YGM41/qEqT4TWohTBTqzy/wLYTqJq2Ij13qlqrVEYGhHCrYSCejjCz651r1jCGqo1GhzCQhg1rq3CrYHqT4T1/6TBTqSCG1jhSCrBeYdBfDTBecr1QVzCQM3Yz65hSD71/Fj1KqJqrhHq/xTCSCUYr6ECeRjYGVg1/u3BeW4YS1rCeCrCQq4CeD/Bf6TyKR3cGYTBTqNYeVgy/u3y/VzYK53YfuT86mm";

            // Act
            var result = _deviceService.GetDeviceInfo(deviceInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.F4);
            Assert.NotNull(result.F5);
            Assert.NotNull(result.Rtc);
            Assert.NotEqual("-1", result.Version);
        }

        [Fact]
        public void GetDeviceInfo_ShouldReturnNullVersion_WhenNewFlowVersionNotExists()
        {
            // Arrange
            var deviceInfo = "P3Rzn3qlq/V4h/VgBeUHCzFU1/I/Y/U5Y/crBeMz1/I7YGM41/qEqT4TcGD2q/xThe641/MShzhHBGVoCfjr1rVEh/cU1/YH1/7rh/qShzMTyKRrCKqlq/hE1rD/hShgYzV5YQqgCS17heYH1eqzCfj/hQarYGDUqT4T1/MTBTqohS631f6HYrV4he7rBfqgCzwz1rhHYShHh/wHYQY5CKqJq2REh3qlq/JdYeULBfqLYeY4y/q4BKRk";

            // Act
            var result = _deviceService.GetDeviceInfo(deviceInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.F4);
            Assert.NotNull(result.F5);
            Assert.NotNull(result.Rtc);
            Assert.Equal("-1", result.Version);
        }

        [Fact]
        public void GetDeviceInfo_ShouldReturnEmptyVersion_WhenNewFlowVersionEmpty()
        {
            // Arrange
            var deviceInfo = "P3Rzn3qlq/V4h/VgBeUHCzFU1/I/Y/U5Y/crBeMz1/I7YGM41/qEqT4TWohTBTqTyKREhQWTBTRjCfFrCe1/C/M5heWEBG1rheITCSIrYzarBQ1TY/1/CvqJqrhEq/xTC/IrhQC/1/UzYeudh/Uo1rajYza7Y/YEBGCj1Qh4hQ6TyKRrCvqlq/c/1fRUCfM3heFjBQh5Y/UoYfCr1/Mz1/aTYfMdhzuEqT4TW2I/q/xTBzVdBv55YT5dYzwLY/w5q2Em";

            // Act
            var result = _deviceService.GetDeviceInfo(deviceInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.F4);
            Assert.NotNull(result.F5);
            Assert.NotNull(result.Rtc);
            Assert.Equal("-2", result.Version);
        }
    }
}
