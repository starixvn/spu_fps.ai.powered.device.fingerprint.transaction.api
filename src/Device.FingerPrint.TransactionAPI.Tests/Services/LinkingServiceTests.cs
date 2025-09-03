using Device.FingerPrint.TransactionAPI.Services;
using Fanex.Logging;
using Moq;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Services
{
    public class LinkingServiceTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IRedisService> _mockRedisService;

        private readonly LinkingService _linkingService;

        public LinkingServiceTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockRedisService = new Mock<IRedisService>();
            _linkingService = new LinkingService(_mockLogger.Object, _mockRedisService.Object);
        }

        [Fact]
        public async Task Link_ShouldReturns()
        {
            await _linkingService.Link("testSession");

            return;
        }
    }
}
