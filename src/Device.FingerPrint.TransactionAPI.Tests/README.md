# RedisService Test Suite Documentation

## Overview

This comprehensive test suite provides thorough coverage for the `RedisService` class and its implementation of the `IRedisService` interface. The test suite is designed to validate functionality, performance, reliability, and edge cases.

## Test Structure

### 1. Core Functionality Tests (`RedisServiceTests.cs`)

**Purpose**: Tests the primary functionality of all RedisService methods.

**Coverage**:
- Property accessibility (RedisDatabase, RedisJSChallengeDatabase)
- Hash operations (HashSetAsync, HashGetAsync, HashExistAsync, HashDeleteAsync)
- Expiry operations (UpdateExpiryAsync, HashSetAndExpiryAsync)
- Simple key-value operations (GetAsync, SetAsync, AddAsyncAndExpiryAsync)
- Increment operations (HashIncrementAsync)
- Retry logic and error handling
- Complex workflow scenarios

**Key Test Scenarios**:
- Normal operation flows
- Force update vs. conditional updates
- Null parameter handling
- Redis connection failures and retry mechanisms
- Logging verification
- Integration-style workflows

### 2. Edge Cases Tests (`RedisServiceEdgeCasesTests.cs`)

**Purpose**: Tests boundary conditions, unusual inputs, and stress scenarios.

**Coverage**:
- Null and empty string parameter handling
- Large data processing (10KB+ strings, 1000+ keys)
- Complex object serialization/deserialization
- DateTime and DateTimeOffset edge cases
- Concurrent access simulation
- Extreme value testing (long.MaxValue, long.MinValue)
- Different Redis exception types
- Progressive retry delays
- Memory leak prevention

**Key Test Scenarios**:
- Null/empty parameter validation
- Large dataset handling
- Complex nested object serialization
- Past/future date handling
- High concurrency simulation
- Resource management verification

### 3. Performance Tests (`RedisServicePerformanceTests.cs`)

**Purpose**: Validates performance characteristics and scalability.

**Coverage**:
- Bulk operation performance (1000+ operations)
- High concurrency performance (50+ concurrent operations)
- Retry logic timing validation
- Memory usage under load
- Throughput measurements
- Stress testing with continuous operations

**Performance Benchmarks**:
- Bulk operations: &lt;5 seconds for 1000 operations
- Mixed operations: &lt;3 seconds for 250 operations (50 concurrent × 5 types)
- Retry logic: &lt;8 seconds for 3 retry attempts
- Memory: &lt;50MB increase for large data operations
- Throughput: &gt;100 operations per second minimum

### 4. Interface Contract Tests (`RedisServiceInterfaceContractTests.cs`)

**Purpose**: Ensures proper implementation of the IRedisService interface.

**Coverage**:
- Interface property accessibility
- Method parameter acceptance
- Generic type support
- Optional parameter handling
- Return type validation
- Method signature compliance

**Key Validations**:
- All interface methods are properly implemented
- Generic constraints work correctly
- Optional parameters have correct defaults
- Return types match interface specifications
- Properties are accessible through interface

### 5. Integration Tests (`RedisServiceIntegrationTests.cs`)

**Purpose**: Simulates real-world usage scenarios and workflows.

**Coverage**:
- User session management workflows
- Cache management with fallback strategies
- Counter/analytics tracking
- Bulk data processing with error handling
- Cross-database operations (default vs. JS Challenge)
- Complex data structure handling
- Cleanup and maintenance operations

**Real-World Scenarios**:
- Session creation, retrieval, extension, and cleanup
- Cache-aside pattern with Redis failures
- Page view counting and analytics
- Bulk user data processing
- Multi-database usage patterns
- Complex object caching strategies

## Test Configuration

### Dependencies
- **xUnit**: Test framework
- **Moq**: Mocking framework for dependencies
- **FluentAssertions**: Expressive assertions
- **StackExchange.Redis.Extensions.Core**: Redis client abstractions

### Test Collections
- **Sequential Collection**: For tests that shouldn't run in parallel
- **Performance Collection**: For tests requiring dedicated resources

### Test Constants
- Defined in `TestConstants` class for consistency
- Standardized test data and timeout values

## Running the Tests

### All Tests
```bash
dotnet test
```

### Specific Test Class
```bash
dotnet test --filter "ClassName=RedisServiceTests"
```

### Performance Tests Only
```bash
dotnet test --filter "Category=Performance"
```

### With Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Data Patterns

### Mock Setup Patterns
- Consistent mock configuration across test classes
- Reusable setup methods for common scenarios
- Proper verification of mock interactions

### Test Data
- Realistic data sizes and formats
- Edge case values (null, empty, extreme values)
- Complex object structures for serialization testing

## Coverage Metrics

The test suite aims for:
- **Line Coverage**: &gt;95%
- **Branch Coverage**: &gt;90%
- **Method Coverage**: 100%

## Maintenance Guidelines

### Adding New Tests
1. Choose appropriate test class based on test purpose
2. Follow existing naming conventions
3. Use setup methods for common mock configurations
4. Include both positive and negative test cases
5. Verify mock interactions where appropriate

### Performance Test Thresholds
- Review performance benchmarks quarterly
- Adjust thresholds based on infrastructure changes
- Monitor test execution time trends

### Mock Updates
- Update mocks when Redis client interfaces change
- Maintain consistency across all test classes
- Document any mock behavior assumptions

## Common Test Patterns

### Arrange-Act-Assert
All tests follow the AAA pattern:
```csharp
// Arrange
const string key = "testKey";
_mockRedisDatabase.Setup(x => x.GetAsync<string>(key))
    .ReturnsAsync("testValue");

// Act
var result = await _redisService.GetAsync<string>(key);

// Assert
result.Should().Be("testValue");
```

### Error Simulation
```csharp
_mockRedisDatabase.SetupSequence(x => x.HashGetAsync<string>(hashKey, key))
    .ThrowsAsync(new RedisConnectionException(...))
    .ReturnsAsync(expectedValue);
```

### Verification Patterns
```csharp
_mockRedisDatabase.Verify(x => x.HashSetAsync<string>(
    hashKey, key, value, It.IsAny<bool>(), It.IsAny<CommandFlags>()), 
    Times.Once);
```

## Best Practices

1. **Test Isolation**: Each test is independent and can run in any order
2. **Clear Naming**: Test names clearly indicate what is being tested
3. **Single Responsibility**: Each test verifies one specific behavior
4. **Comprehensive Coverage**: Tests cover happy path, edge cases, and error conditions
5. **Performance Awareness**: Performance tests validate non-functional requirements
6. **Documentation**: Complex test scenarios are well-documented

This test suite provides confidence in the RedisService implementation and helps ensure reliable operation in production environments.