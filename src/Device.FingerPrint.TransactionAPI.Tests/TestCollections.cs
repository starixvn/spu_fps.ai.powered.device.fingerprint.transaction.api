using Xunit;

// Assembly level test configuration
[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = 4)]

namespace Device.FingerPrint.TransactionAPI.Tests
{
    /// <summary>
    /// Test collection definitions for organizing tests
    /// </summary>
    public class TestCollections
    {
        /// <summary>
        /// Collection for Redis service tests that should not run in parallel
        /// </summary>
        [CollectionDefinition("RedisService Sequential")]
        public class RedisServiceSequentialCollection : ICollectionFixture<RedisServiceTestFixture>
        {
        }

        /// <summary>
        /// Collection for performance tests that may require dedicated resources
        /// </summary>
        [CollectionDefinition("Performance Tests")]
        public class PerformanceTestCollection : ICollectionFixture<PerformanceTestFixture>
        {
        }
    }

    /// <summary>
    /// Shared test fixture for Redis service tests
    /// </summary>
    public class RedisServiceTestFixture : IDisposable
    {
        public RedisServiceTestFixture()
        {
            // Initialize any shared resources here
        }

        public void Dispose()
        {
            // Cleanup shared resources here
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Shared test fixture for performance tests
    /// </summary>
    public class PerformanceTestFixture : IDisposable
    {
        public PerformanceTestFixture()
        {
            // Initialize performance test resources
        }

        public void Dispose()
        {
            // Cleanup performance test resources
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Test constants and shared utilities
    /// </summary>
    public static class TestConstants
    {
        public const string TestHashKey = "test:hash";
        public const string TestKey = "test:key";
        public const string TestValue = "test:value";
        public const int DefaultTimeoutMs = 5000;
        public const int PerformanceTestIterations = 1000;
    }
}