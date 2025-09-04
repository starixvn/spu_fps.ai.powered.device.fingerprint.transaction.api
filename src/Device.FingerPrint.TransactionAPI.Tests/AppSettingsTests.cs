using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Device.FingerPrint.TransactionAPI.Tests
{
    /// <summary>
    /// Tests for AppSettings configuration class
    /// </summary>
    public class AppSettingsTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Dictionary<string, string> _configurationData;

        public AppSettingsTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _configurationData = new Dictionary<string, string>();
            
            SetupConfigurationMock();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidConfiguration_ShouldInitializeAllProperties()
        {
            // Arrange
            SetupDefaultConfigurationValues();

            // Act
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Assert
            appSettings.Environment.Should().Be("TEST");
            appSettings.QueueName.Should().Be("test.queue");
            appSettings.MobileQueueName.Should().Be("test.mobile.queue");
            appSettings.IsUM.Should().BeTrue();
            appSettings.SentryDsn.Should().Be("https://test@sentry.io/123");
            appSettings.IgnoredSites.Should().Be("test1,test2");
            appSettings.GetSubscriberInfo.Should().Be("https://api.test.com/subscriber");
            appSettings.FPJSPortialAPI.Should().Be("https://fpjs.test.com");
            appSettings.JWTPassword.Should().Be("testpassword");
            appSettings.JWTEnable.Should().BeTrue();
            appSettings.RedisDefaultDatabase.Should().Be(14);
        }

        [Fact]
        public void Constructor_WithMissingRequiredValues_ShouldThrowException()
        {
            // Arrange
            // Clear any existing configuration data and don't setup default values
            _configurationData.Clear();
            
            // The existing mock setup in constructor will return null for missing keys
            // which should cause TypeConverter to fail when converting null values to non-string types

            // Act & Assert
            var exception = Assert.Throws<InvalidCastException>(() => new AppSettings(_mockConfiguration.Object));
            // The first non-string property that fails conversion is IsUM (boolean)
            exception.Message.Should().Contain("Key: IsUM");
        }

        #endregion

        #region Property Tests

        [Theory]
        [InlineData("LOCAL")]
        [InlineData("DEV")]
        [InlineData("UAT")]
        [InlineData("PROD")]
        public void Environment_WithDifferentValues_ShouldReturnCorrectValue(string environment)
        {
            // Arrange
            SetupConfigurationValue("Environment", environment);
            SetupDefaultConfigurationValues();
            _configurationData["Environment"] = environment;

            // Act
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Assert
            appSettings.Environment.Should().Be(environment);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("True", true)]
        [InlineData("False", false)]
        [InlineData("1", true)]
        [InlineData("0", false)]
        public void IsUM_WithDifferentBooleanValues_ShouldReturnCorrectValue(string configValue, bool expectedValue)
        {
            // Arrange
            SetupDefaultConfigurationValues();
            _configurationData["IsUM"] = configValue;

            // Act
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Assert
            appSettings.IsUM.Should().Be(expectedValue);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public void JWTEnable_WithBooleanValues_ShouldReturnCorrectValue(string configValue, bool expectedValue)
        {
            // Arrange
            SetupDefaultConfigurationValues();
            _configurationData["JWT:Enable"] = configValue;

            // Act
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Assert
            appSettings.JWTEnable.Should().Be(expectedValue);
        }

        [Fact]
        public void QueueName_WithRabbitMQConfiguration_ShouldReturnCorrectValue()
        {
            // Arrange
            const string expectedQueueName = "production.fingerprint.queue";
            SetupDefaultConfigurationValues();
            _configurationData["RabbitMQ:Queue"] = expectedQueueName;

            // Act
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Assert
            appSettings.QueueName.Should().Be(expectedQueueName);
        }

        [Fact]
        public void MobileQueueName_WithRabbitMQConfiguration_ShouldReturnCorrectValue()
        {
            // Arrange
            const string expectedMobileQueueName = "production.mobile.queue";
            SetupDefaultConfigurationValues();
            _configurationData["RabbitMQ:MobileQueue"] = expectedMobileQueueName;

            // Act
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Assert
            appSettings.MobileQueueName.Should().Be(expectedMobileQueueName);
        }

        #endregion

        #region GetValue Method Tests

        [Fact]
        public void GetValue_WithValidStringKey_ShouldReturnStringValue()
        {
            // Arrange
            SetupDefaultConfigurationValues();
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Act
            var result = appSettings.GetValue<string>("Environment");

            // Assert
            result.Should().Be("TEST");
        }

        [Fact]
        public void GetValue_WithValidBoolKey_ShouldReturnBoolValue()
        {
            // Arrange
            SetupDefaultConfigurationValues();
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Act
            var result = appSettings.GetValue<bool>("IsUM");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetValue_WithValidIntKey_ShouldReturnIntValue()
        {
            // Arrange
            SetupDefaultConfigurationValues();
            _configurationData["TestInt"] = "42";
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Act
            var result = appSettings.GetValue<int>("TestInt");

            // Assert
            result.Should().Be(42);
        }

        [Fact]
        public void GetValue_WithInvalidKey_ShouldThrowInvalidCastException()
        {
            // Arrange
            SetupDefaultConfigurationValues();
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Act & Assert
            // Trying to get a non-string type from a non-existent key should throw an exception
            // because TypeConverter cannot convert null to int
            var exception = Assert.Throws<InvalidCastException>(() => appSettings.GetValue<int>("NonExistentKey"));
            exception.Message.Should().Contain("Key: NonExistentKey");
        }

        [Fact]
        public void GetValue_WithInvalidTypeConversion_ShouldThrowInvalidCastException()
        {
            // Arrange
            SetupDefaultConfigurationValues();
            _configurationData["InvalidNumber"] = "not-a-number";
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Act & Assert
            var exception = Assert.Throws<InvalidCastException>(() => appSettings.GetValue<int>("InvalidNumber"));
            exception.Message.Should().Contain("Key: InvalidNumber");
        }

        #endregion

        #region Edge Cases Tests

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithEmptyOrWhitespaceValues_ShouldAcceptValues(string emptyValue)
        {
            // Arrange
            SetupDefaultConfigurationValues();
            _configurationData["Environment"] = emptyValue;

            // Act
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Assert
            appSettings.Environment.Should().Be(emptyValue);
        }

        [Fact]
        public void Constructor_WithNullConfigurationValues_ShouldThrowException()
        {
            // Arrange
            _mockConfiguration.Setup(x => x[It.IsAny<string>()])
                .Returns((string?)null);

            // Act & Assert
            Assert.Throws<InvalidCastException>(() => new AppSettings(_mockConfiguration.Object));
        }

        [Fact]
        public void IgnoredSites_WithWildcardValue_ShouldReturnWildcard()
        {
            // Arrange
            SetupDefaultConfigurationValues();
            _configurationData["IgnoredSites"] = "*";

            // Act
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Assert
            appSettings.IgnoredSites.Should().Be("*");
        }

        [Fact]
        public void IgnoredSites_WithMultipleSites_ShouldReturnCommaSeparatedList()
        {
            // Arrange
            const string ignoredSites = "site1.com,site2.com,site3.com";
            SetupDefaultConfigurationValues();
            _configurationData["IgnoredSites"] = ignoredSites;

            // Act
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Assert
            appSettings.IgnoredSites.Should().Be(ignoredSites);
        }

        #endregion

        #region URL Configuration Tests

        [Theory]
        [InlineData("https://api.production.com/subscriber")]
        [InlineData("http://localhost:5000/api/subscriber")]
        [InlineData("https://api-dev.company.com/v1/subscriber")]
        public void GetSubscriberInfo_WithDifferentURLFormats_ShouldReturnCorrectURL(string url)
        {
            // Arrange
            SetupDefaultConfigurationValues();
            _configurationData["GetSubscriberUrl"] = url;

            // Act
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Assert
            appSettings.GetSubscriberInfo.Should().Be(url);
        }

        [Theory]
        [InlineData("https://js.fingerprint.com/v3")]
        [InlineData("https://cdn.jsdelivr.net/npm/@fingerprintjs/fingerprintjs@3")]
        public void FPJSPortialAPI_WithDifferentURLs_ShouldReturnCorrectURL(string url)
        {
            // Arrange
            SetupDefaultConfigurationValues();
            _configurationData["FPJS-Portial-API"] = url;

            // Act
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Assert
            appSettings.FPJSPortialAPI.Should().Be(url);
        }

        #endregion

        #region Security Configuration Tests

        [Theory]
        [InlineData("simplepassword")]
        [InlineData("C0mpl3x!P@ssw0rd")]
        [InlineData("very-long-password-with-special-characters-!@#$%^&*()")]
        public void JWTPassword_WithDifferentPasswords_ShouldReturnCorrectPassword(string password)
        {
            // Arrange
            SetupDefaultConfigurationValues();
            _configurationData["JWT:Password"] = password;

            // Act
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Assert
            appSettings.JWTPassword.Should().Be(password);
        }

        [Theory]
        [InlineData("https://abc123@o123456.ingest.sentry.io/123456")]
        [InlineData("https://def456@sentry.custom-domain.com/789")]
        public void SentryDsn_WithDifferentDSNFormats_ShouldReturnCorrectDSN(string dsn)
        {
            // Arrange
            SetupDefaultConfigurationValues();
            _configurationData["Sentry:Dsn"] = dsn;

            // Act
            var appSettings = new AppSettings(_mockConfiguration.Object);

            // Assert
            appSettings.SentryDsn.Should().Be(dsn);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Constructor_MultipleInstances_ShouldInitializeQuickly()
        {
            // Arrange
            SetupDefaultConfigurationValues();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 100; i++)
            {
                var appSettings = new AppSettings(_mockConfiguration.Object);
            }

            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete in less than 1 second
        }

        #endregion

        #region Helper Methods

        private void SetupConfigurationMock()
        {
            _mockConfiguration.Setup(x => x[It.IsAny<string>()])
                .Returns<string>(key => _configurationData.TryGetValue(key, out var value) ? value : null);
        }

        private void SetupConfigurationValue(string key, string value)
        {
            _configurationData[key] = value;
        }

        private void SetupDefaultConfigurationValues()
        {
            _configurationData["Environment"] = "TEST";
            _configurationData["RabbitMQ:Queue"] = "test.queue";
            _configurationData["RabbitMQ:MobileQueue"] = "test.mobile.queue";
            _configurationData["IsUM"] = "true";
            _configurationData["Sentry:Dsn"] = "https://test@sentry.io/123";
            _configurationData["IgnoredSites"] = "test1,test2";
            _configurationData["GetSubscriberUrl"] = "https://api.test.com/subscriber";
            _configurationData["FPJS-Portial-API"] = "https://fpjs.test.com";
            _configurationData["JWT:Password"] = "testpassword";
            _configurationData["JWT:Enable"] = "true";
            _configurationData["Redis:Database"] = "14";
        }

        #endregion
    }
}