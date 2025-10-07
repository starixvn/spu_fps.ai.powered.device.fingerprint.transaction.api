using Device.FingerPrint.TransactionAPI.Controllers;
using FluentAssertions;
using Moq;
using ONEbook.UM.Models;
using ONEbook.UM.Services;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Controllers
{
    public class UMControllerTests
    {
        private readonly Mock<IMaintenanceService> _mockMaintenanceService;
        private readonly UMController _controller;

        public UMControllerTests()
        {
            _mockMaintenanceService = new Mock<IMaintenanceService>();
            _controller = new UMController(_mockMaintenanceService.Object);
        }

        [Fact]
        public void TestUM_ShouldReturnMaintenanceTimeFromService()
        {
            // Arrange
            var expectedMaintenanceTime = new UnderMaintenanceTime
            {
                IsUnderMaintenanceTime = true
            };

            _mockMaintenanceService.Setup(x => x.GetUnderMaintenanceTime())
                .Returns(expectedMaintenanceTime);

            // Act
            var result = _controller.TestUM();

            // Assert
            result.Should().BeEquivalentTo(expectedMaintenanceTime);
            _mockMaintenanceService.Verify(x => x.GetUnderMaintenanceTime(), Times.Once);
        }

        [Fact]
        public void TestUM_WithNullMaintenanceTime_ShouldReturnNull()
        {
            // Arrange
            _mockMaintenanceService.Setup(x => x.GetUnderMaintenanceTime())
                .Returns((UnderMaintenanceTime?)null);

            // Act
            var result = _controller.TestUM();

            // Assert
            result.Should().BeNull();
        }

    }
}
