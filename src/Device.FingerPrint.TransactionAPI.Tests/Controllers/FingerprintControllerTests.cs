using Device.FingerPrint.TransactionAPI.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Controllers
{
    public class FingerprintControllerTests
    {
        private readonly FingerprintController _controller;
        public FingerprintControllerTests()
        {
            _controller = new FingerprintController();
        }
        [Fact]
        public void InsertFP4_ShouldReturnOk()
        {
            // Arrange
            var fingerprintRequest = new FingerprintRequest() { SessionKey = "test-session-key", Hashed = "Hashed", FingerPrint = new Fingperprint()};

            // Act
            var result = _controller.InsertFP4(fingerprintRequest);

            // Assert
            result.Should().BeOfType<OkResult>();
        }

        [Fact]
        public void InsertFP5_ShouldReturnOk()
        {
            // Arrange
            var fingerprintRequest = new FingerprintRequest() { SessionKey = "test-session-key", Hashed = "Hashed", FingerPrint = new Fingperprint() };

            // Act
            var result = _controller.InsertFP5(fingerprintRequest);

            // Assert
            result.Should().BeOfType<OkResult>();
        }
    }
}
