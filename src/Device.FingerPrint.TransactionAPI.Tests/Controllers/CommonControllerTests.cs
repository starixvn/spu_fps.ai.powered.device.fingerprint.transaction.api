using Device.FingerPrint.TransactionAPI.Controllers;
using Device.FingerPrint.TransactionAPI.Model;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests.Controllers
{
    /// <summary>
    /// Comprehensive test suite for CommonController
    /// Tests the whoami endpoint which provides server information including:
    /// - Environment configuration
    /// - Server name and machine information
    /// - Server time
    /// - Local IP address from HTTP connection features
    /// </summary>
    public class CommonControllerTests
    {
        private readonly Mock<IAppSettings> _mockAppSettings;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<IFeatureCollection> _mockFeatures;
        private readonly Mock<IHttpConnectionFeature> _mockConnectionFeature;
        private readonly CommonController _controller;

        public CommonControllerTests()
        {
            _mockAppSettings = new Mock<IAppSettings>();
            _mockHttpContext = new Mock<HttpContext>();
            _mockFeatures = new Mock<IFeatureCollection>();
            _mockConnectionFeature = new Mock<IHttpConnectionFeature>();

            // Setup default app settings
            _mockAppSettings.Setup(x => x.Environment).Returns("TEST");

            // Create controller with mocked dependencies
            _controller = new CommonController(_mockAppSettings.Object);

            // Setup HttpContext mock chain
            _mockHttpContext.Setup(x => x.Features).Returns(_mockFeatures.Object);
            _mockFeatures.Setup(x => x.Get<IHttpConnectionFeature>()).Returns(_mockConnectionFeature.Object);

            // Assign mocked HttpContext to controller
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            };
        }

        #region Index Method Tests

        [Fact]
        public void Index_WithValidConfiguration_ShouldReturnCompleteWhoamiModel()
        {
            // Arrange
            var expectedEnvironment = "PRODUCTION";
            var expectedLocalIp = IPAddress.Parse("192.168.1.100");
            
            _mockAppSettings.Setup(x => x.Environment).Returns(expectedEnvironment);
            _mockConnectionFeature.Setup(x => x.LocalIpAddress).Returns(expectedLocalIp);

            var beforeCall = DateTime.Now;

            // Act
            var result = _controller.Index();

            var afterCall = DateTime.Now;

            // Assert
            result.Should().NotBeNull();
            result.Environment.Should().Be(expectedEnvironment);
            result.ServerName.Should().Be(Environment.MachineName);
            result.LocalIP.Should().Be(expectedLocalIp.ToString());
            
            // Verify ServerTime is recent and reasonable
            result.ServerTime.Should().BeOnOrAfter(beforeCall.AddSeconds(-1));
            result.ServerTime.Should().BeOnOrBefore(afterCall.AddSeconds(1));
        }

        [Fact]
        public void Index_WithNullLocalIpAddress_ShouldHandleGracefully()
        {
            // Arrange
            var expectedEnvironment = "TEST";
            
            _mockAppSettings.Setup(x => x.Environment).Returns(expectedEnvironment);
            _mockConnectionFeature.Setup(x => x.LocalIpAddress).Returns((IPAddress?)null);

            // Act
            var result = _controller.Index();

            // Assert
            result.Should().NotBeNull();
            result.Environment.Should().Be(expectedEnvironment);
            result.ServerName.Should().Be(Environment.MachineName);
            result.LocalIP.Should().BeNull();
            result.ServerTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Index_WithNullConnectionFeature_ShouldHandleGracefully()
        {
            // Arrange
            var expectedEnvironment = "DEV";
            
            _mockAppSettings.Setup(x => x.Environment).Returns(expectedEnvironment);
            _mockFeatures.Setup(x => x.Get<IHttpConnectionFeature>()).Returns((IHttpConnectionFeature?)null);

            // Act
            var result = _controller.Index();

            // Assert
            result.Should().NotBeNull();
            result.Environment.Should().Be(expectedEnvironment);
            result.ServerName.Should().Be(Environment.MachineName);
            result.LocalIP.Should().BeNull();
            result.ServerTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Theory]
        [InlineData("LOCAL")]
        [InlineData("DEV")]
        [InlineData("UAT")]
        [InlineData("STAGING")]
        [InlineData("PRODUCTION")]
        [InlineData("")]
        [InlineData(null)]
        public void Index_WithDifferentEnvironments_ShouldReturnCorrectEnvironment(string? environment)
        {
            // Arrange
            _mockAppSettings.Setup(x => x.Environment).Returns(environment!);
            _mockConnectionFeature.Setup(x => x.LocalIpAddress).Returns(IPAddress.Loopback);

            // Act
            var result = _controller.Index();

            // Assert
            result.Should().NotBeNull();
            result.Environment.Should().Be(environment);
        }

        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("192.168.1.1")]
        [InlineData("10.0.0.1")]
        [InlineData("172.16.0.1")]
        [InlineData("::1")] // IPv6 loopback
        [InlineData("2001:db8::1")] // IPv6 example
        public void Index_WithDifferentIpAddresses_ShouldReturnCorrectLocalIP(string ipAddressString)
        {
            // Arrange
            var ipAddress = IPAddress.Parse(ipAddressString);
            var expectedEnvironment = "TEST";
            
            _mockAppSettings.Setup(x => x.Environment).Returns(expectedEnvironment);
            _mockConnectionFeature.Setup(x => x.LocalIpAddress).Returns(ipAddress);

            // Act
            var result = _controller.Index();

            // Assert
            result.Should().NotBeNull();
            result.LocalIP.Should().Be(ipAddress.ToString());
            result.Environment.Should().Be(expectedEnvironment);
        }

        [Fact]
        public void Index_ServerName_ShouldAlwaysReturnMachineName()
        {
            // Arrange
            var expectedMachineName = Environment.MachineName;
            _mockAppSettings.Setup(x => x.Environment).Returns("TEST");

            // Act
            var result = _controller.Index();

            // Assert
            result.Should().NotBeNull();
            result.ServerName.Should().Be(expectedMachineName);
            result.ServerName.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Index_ServerTime_ShouldBeCurrentLocalTime()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.Environment).Returns("TEST");
            var beforeCall = DateTimeOffset.Now;

            // Act
            var result = _controller.Index();

            var afterCall = DateTimeOffset.Now;

            // Assert
            result.Should().NotBeNull();
            result.ServerTime.Should().BeOnOrAfter(beforeCall);
            result.ServerTime.Should().BeOnOrBefore(afterCall);

            // Verify it's local time, not UTC
            result.ServerTime.Offset.Should().Be(TimeZoneInfo.Local.GetUtcOffset(result.ServerTime));
        }

        #endregion

        #region WhoamiModel Property Tests

        [Fact]
        public void Index_ShouldReturnWhoamiModelWithAllAccessibleProperties()
        {
            // Arrange
            var expectedEnvironment = "FULL_TEST";
            var expectedLocalIp = IPAddress.Parse("203.0.113.1");
            
            _mockAppSettings.Setup(x => x.Environment).Returns(expectedEnvironment);
            _mockConnectionFeature.Setup(x => x.LocalIpAddress).Returns(expectedLocalIp);

            // Act
            var result = _controller.Index();

            // Assert
            result.Should().NotBeNull();
            
            // Verify all properties that are set by the controller
            result.Environment.Should().Be(expectedEnvironment);
            result.ServerName.Should().Be(Environment.MachineName);
            result.LocalIP.Should().Be(expectedLocalIp.ToString());
            result.ServerTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
            
            // Verify properties that are not set by this controller remain null
            result.RealIP.Should().BeNull();
            result.FakeIP.Should().BeNull();
            result.XFW.Should().BeNull();
            result.ClientIP.Should().BeNull();
            result.ActualIP.Should().BeNull();
        }

        [Fact]
        public void Index_WhoamiModel_ShouldBeSerializableToJson()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.Environment).Returns("JSON_TEST");
            _mockConnectionFeature.Setup(x => x.LocalIpAddress).Returns(IPAddress.Loopback);

            // Act
            var result = _controller.Index();

            // Assert
            result.Should().NotBeNull();
            
            // Test JSON serialization
            var jsonResult = System.Text.Json.JsonSerializer.Serialize(result);
            jsonResult.Should().NotBeNullOrEmpty();
            jsonResult.Should().Contain("JSON_TEST");
            jsonResult.Should().Contain(Environment.MachineName);
            jsonResult.Should().Contain("127.0.0.1");
        }

        #endregion

        #region Error Handling and Edge Cases

        [Fact]
        public void Index_WithHttpContextFeatureException_ShouldHandleGracefully()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.Environment).Returns("ERROR_TEST");
            _mockFeatures.Setup(x => x.Get<IHttpConnectionFeature>())
                .Throws(new InvalidOperationException("Feature collection error"));

            // Act
            var result = _controller.Index();

            // Assert
            result.Should().NotBeNull();
            result.Environment.Should().Be("ERROR_TEST");
            result.ServerName.Should().Be(Environment.MachineName);
            result.LocalIP.Should().BeNull(); // Should be null due to exception
            result.ServerTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Index_WithConnectionFeatureException_ShouldHandleGracefully()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.Environment).Returns("CONNECTION_ERROR_TEST");
            _mockConnectionFeature.Setup(x => x.LocalIpAddress)
                .Throws(new NotSupportedException("IP address not available"));

            // Act
            var result = _controller.Index();

            // Assert
            result.Should().NotBeNull();
            result.Environment.Should().Be("CONNECTION_ERROR_TEST");
            result.ServerName.Should().Be(Environment.MachineName);
            result.LocalIP.Should().BeNull(); // Should be null due to exception
            result.ServerTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Index_WithNullAppSettingsEnvironment_ShouldHandleGracefully()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.Environment).Returns((string?)null!);
            _mockConnectionFeature.Setup(x => x.LocalIpAddress).Returns(IPAddress.Loopback);

            // Act
            var result = _controller.Index();

            // Assert
            result.Should().NotBeNull();
            result.Environment.Should().BeNull();
            result.ServerName.Should().Be(Environment.MachineName);
            result.LocalIP.Should().Be("127.0.0.1");
            result.ServerTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        #endregion

        #region Integration and Real-World Scenarios

        [Fact]
        public void Index_MultipleCallsInSequence_ShouldReturnConsistentResults()
        {
            // Arrange
            var expectedEnvironment = "CONSISTENCY_TEST";
            var expectedLocalIp = IPAddress.Parse("10.0.0.100");
            
            _mockAppSettings.Setup(x => x.Environment).Returns(expectedEnvironment);
            _mockConnectionFeature.Setup(x => x.LocalIpAddress).Returns(expectedLocalIp);

            // Act - Use Task.Delay instead of Thread.Sleep for better test practices
            var result1 = _controller.Index();
            var delayTask1 = Task.Delay(5); // Small delay to ensure different timestamps
            delayTask1.Wait();
            var result2 = _controller.Index();
            var delayTask2 = Task.Delay(5);
            delayTask2.Wait();
            var result3 = _controller.Index();

            // Assert
            var results = new[] { result1, result2, result3 };
            
            foreach (var result in results)
            {
                result.Should().NotBeNull();
                result.Environment.Should().Be(expectedEnvironment);
                result.ServerName.Should().Be(Environment.MachineName);
                result.LocalIP.Should().Be(expectedLocalIp.ToString());
            }
            
            // Verify timestamps are progressive (allow for some tolerance due to system timing)
            result1.ServerTime.Should().BeBefore(result2.ServerTime.AddMilliseconds(1));
            result2.ServerTime.Should().BeBefore(result3.ServerTime.AddMilliseconds(1));
        }

        [Fact]
        public void Index_ConcurrentCalls_ShouldHandleCorrectly()
        {
            // Arrange
            var expectedEnvironment = "CONCURRENT_TEST";
            var expectedLocalIp = IPAddress.Parse("172.16.1.50");
            
            _mockAppSettings.Setup(x => x.Environment).Returns(expectedEnvironment);
            _mockConnectionFeature.Setup(x => x.LocalIpAddress).Returns(expectedLocalIp);

            var tasks = new List<Task<WhoamiModel>>();
            const int concurrentCalls = 10;

            // Act
            for (int i = 0; i < concurrentCalls; i++)
            {
                tasks.Add(Task.Run(() => _controller.Index()));
            }

            var results = Task.WhenAll(tasks).Result;

            // Assert
            results.Should().HaveCount(concurrentCalls);
            
            foreach (var result in results)
            {
                result.Should().NotBeNull();
                result.Environment.Should().Be(expectedEnvironment);
                result.ServerName.Should().Be(Environment.MachineName);
                result.LocalIP.Should().Be(expectedLocalIp.ToString());
                result.ServerTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(10));
            }
        }

        [Fact]
        public void Index_WithRealWorldIpAddresses_ShouldFormatCorrectly()
        {
            // Arrange
            var testCases = new (string IpString, string Description)[]
            {
                ("127.0.0.1", "Loopback IPv4"),
                ("0.0.0.0", "Any IPv4"),
                ("255.255.255.255", "Broadcast IPv4"),
                ("::1", "Loopback IPv6"),
                ("::", "Any IPv6"),
                ("2001:db8:85a3::8a2e:370:7334", "Full IPv6")
            };

            foreach (var (ipString, description) in testCases)
            {
                // Arrange
                var ipAddress = IPAddress.Parse(ipString);
                _mockAppSettings.Setup(x => x.Environment).Returns($"TEST_{description.Replace(" ", "_")}");
                _mockConnectionFeature.Setup(x => x.LocalIpAddress).Returns(ipAddress);

                // Act
                var result = _controller.Index();

                // Assert
                result.Should().NotBeNull($"Failed for {description}");
                result.LocalIP.Should().Be(ipAddress.ToString(), $"IP formatting failed for {description}");
            }
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Index_PerformanceBenchmark_ShouldExecuteQuickly()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.Environment).Returns("PERFORMANCE_TEST");
            _mockConnectionFeature.Setup(x => x.LocalIpAddress).Returns(IPAddress.Loopback);

            const int iterations = 1000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var result = _controller.Index();
                result.Should().NotBeNull(); // Ensure the result is valid
            }

            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "1000 calls should complete within 1 second");
            
            // Calculate average time per call
            var averageTimePerCall = (double)stopwatch.ElapsedMilliseconds / iterations;
            averageTimePerCall.Should().BeLessThan(1.0, "Each call should take less than 1ms on average");
        }

        #endregion

        #region Controller Infrastructure Tests

        [Fact]
        public void Constructor_WithValidAppSettings_ShouldInitializeCorrectly()
        {
            // Arrange
            var mockAppSettings = new Mock<IAppSettings>();
            mockAppSettings.Setup(x => x.Environment).Returns("CONSTRUCTOR_TEST");

            // Act
            var controller = new CommonController(mockAppSettings.Object);

            // Assert
            controller.Should().NotBeNull();
            
            // Since the controller uses primary constructor syntax, we can't directly access the field
            // But we can test by calling the Index method
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            };

            var result = controller.Index();
            result.Environment.Should().Be("CONSTRUCTOR_TEST");
        }

        [Fact]
        public void Index_WithoutHttpContext_ShouldHandleGracefully()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.Environment).Returns("NO_CONTEXT_TEST");
            
            // Create controller without setting HttpContext
            var controllerWithoutContext = new CommonController(_mockAppSettings.Object);

            // Act & Assert
            // This should not throw an exception, but LocalIP will be null
            var result = controllerWithoutContext.Index();
            
            result.Should().NotBeNull();
            result.Environment.Should().Be("NO_CONTEXT_TEST");
            result.ServerName.Should().Be(Environment.MachineName);
            result.LocalIP.Should().BeNull();
            result.ServerTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        #endregion

        #region HTTP Route and Attribute Tests

        [Fact]
        public void Controller_ShouldHaveApiControllerAttribute()
        {
            // Act
            var controllerType = typeof(CommonController);
            var attributes = controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false);

            // Assert
            attributes.Should().NotBeEmpty("Controller should have ApiController attribute");
        }

        [Fact]
        public void Index_ShouldHaveCorrectHttpGetAttribute()
        {
            // Act
            var methodInfo = typeof(CommonController).GetMethod(nameof(CommonController.Index));
            var attributes = methodInfo?.GetCustomAttributes(typeof(HttpGetAttribute), false);

            // Assert
            methodInfo.Should().NotBeNull();
            attributes.Should().NotBeEmpty("Index method should have HttpGet attribute");
            
            var httpGetAttribute = attributes?.FirstOrDefault() as HttpGetAttribute;
            httpGetAttribute?.Template.Should().Be("whoami", "Route should be 'whoami'");
        }

        #endregion

        #region Edge Cases and Boundary Tests

        [Fact]
        public void Index_WithNullHttpContext_ShouldHandleGracefully()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.Environment).Returns("NULL_CONTEXT_TEST");
            
            var controller = new CommonController(_mockAppSettings.Object);
            // Don't set ControllerContext, leaving HttpContext as null

            // Act
            var result = controller.Index();

            // Assert
            result.Should().NotBeNull();
            result.Environment.Should().Be("NULL_CONTEXT_TEST");
            result.ServerName.Should().Be(Environment.MachineName);
            result.LocalIP.Should().BeNull();
            result.ServerTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Index_WithNullFeatures_ShouldHandleGracefully()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.Environment).Returns("NULL_FEATURES_TEST");
            _mockHttpContext.Setup(x => x.Features).Returns((IFeatureCollection?)null!);

            // Act
            var result = _controller.Index();

            // Assert
            result.Should().NotBeNull();
            result.Environment.Should().Be("NULL_FEATURES_TEST");
            result.ServerName.Should().Be(Environment.MachineName);
            result.LocalIP.Should().BeNull();
            result.ServerTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Index_ServerName_ShouldBeCachedValue()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.Environment).Returns("CACHE_TEST");
            var expectedServerName = Environment.MachineName;

            // Act - Call multiple times
            var result1 = _controller.Index();
            var result2 = _controller.Index();
            var result3 = _controller.Index();

            // Assert - All should return the same cached server name
            result1.ServerName.Should().Be(expectedServerName);
            result2.ServerName.Should().Be(expectedServerName);
            result3.ServerName.Should().Be(expectedServerName);
            
            // Verify they're all the same reference (cached)
            result1.ServerName.Should().BeSameAs(result2.ServerName);
            result2.ServerName.Should().BeSameAs(result3.ServerName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public void Index_WithWhitespaceEnvironment_ShouldHandleCorrectly(string environment)
        {
            // Arrange
            _mockAppSettings.Setup(x => x.Environment).Returns(environment);
            _mockConnectionFeature.Setup(x => x.LocalIpAddress).Returns(IPAddress.Loopback);

            // Act
            var result = _controller.Index();

            // Assert
            result.Should().NotBeNull();
            result.Environment.Should().Be(environment);
            result.ServerName.Should().Be(Environment.MachineName);
            result.LocalIP.Should().Be("127.0.0.1");
        }

        #endregion

        #region Time-Based Tests (Improved to avoid Thread.Sleep)

        [Fact]
        public void Index_ServerTime_ShouldBeReasonablyAccurate()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.Environment).Returns("TIME_TEST");
            
            // Act - Capture time before and after with minimal delay
            var beforeTime = DateTime.Now;
            var result = _controller.Index();
            var afterTime = DateTime.Now;

            // Assert
            result.Should().NotBeNull();
            result.ServerTime.Should().BeOnOrAfter(beforeTime);
            result.ServerTime.Should().BeOnOrBefore(afterTime);
            
            // Verify the time is within a reasonable range (should be almost instant)
            var timeDifference = afterTime - beforeTime;
            timeDifference.Should().BeLessThan(TimeSpan.FromMilliseconds(100), 
                "Method execution should be very fast");
        }

        [Fact]
        public async Task Index_TimestampProgression_ShouldBeMonotonic()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.Environment).Returns("MONOTONIC_TEST");
            
            // Act - Get multiple timestamps with async delays instead of Thread.Sleep
            var result1 = _controller.Index();
            await Task.Delay(1); // Minimal async delay
            var result2 = _controller.Index();
            await Task.Delay(1);
            var result3 = _controller.Index();

            // Assert
            result1.ServerTime.Should().BeBefore(result2.ServerTime.AddMilliseconds(10));
            result2.ServerTime.Should().BeBefore(result3.ServerTime.AddMilliseconds(10));
            
            // All results should be recent
            var now = DateTime.Now;
            result1.ServerTime.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
            result2.ServerTime.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
            result3.ServerTime.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        }

        #endregion
    }
}