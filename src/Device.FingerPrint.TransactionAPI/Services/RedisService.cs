using Device.FingerPrint.TransactionAPI.Model;
using MassTransit.Logging;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface IRedisService
    {
        IRedisDatabase RedisDatabase { get; }

        IRedisDatabase RedisShadowDatabase { get; }

        Task<T?> HashGetAsync<T>(string hashKey, string key);

        Task<bool> HashSetAsync<T>(string hashKey, string key, T value, bool isForceUpdate);

        Task<bool> HashExistAsync<T>(string hashKey, string key);

        Task<bool> UpdateExpiryAsync(string key, DateTimeOffset expiresAt);

        Task<long> HashDeleteAsync(string hashKey, string[] keys);

        Task HashSetAndExpiryAsync<T>(string hashKey, string key, T value, DateTimeOffset expiresAt, bool isForceUpdate = false);

        Task<T> GetAsync<T>(string key);

        Task<bool> SetAsync<T>(string key, T value);

        Task<bool> AddAsyncAndExpiryAsync<T>(string key, T value, DateTimeOffset expiresAt);

        Task<long> HashIncrementAsync(string hashKey, string key, long value);
    }

    public class RedisService : IRedisService
    {
        private readonly IRedisDatabase redisDatabase;
        private readonly IRedisDatabase redisShadowDatabase;
        private readonly ILogger _logger;
        private readonly RedisSettings _redisSettings;

        public RedisService(IAppSettings appSettings, IRedisClient redisClient, ILogger logger)
        {
            _redisSettings = appSettings.RedisSettings;
            redisDatabase = redisClient.GetDefaultDatabase();
            redisShadowDatabase = redisClient.GetDb(_redisSettings.ShadowDatabase);
            _logger = logger;
        }

        public IRedisDatabase RedisDatabase => redisDatabase;

        public IRedisDatabase RedisShadowDatabase => redisShadowDatabase;

        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
        {
            for (int attempt = 1; attempt <= _redisSettings.MaxRetryAttempts; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (RedisConnectionException ex)
                {
                    await _logger.WarnAsync( $"Redis connection error on attempt {attempt} for operation {operationName}", ex);

                    if (attempt == _redisSettings.MaxRetryAttempts)
                    {
                        await _logger.ErrorAsync($"Redis operation {operationName} failed after {_redisSettings.MaxRetryAttempts} attempts", ex);
                        throw;
                    }

                    await Task.Delay(_redisSettings.RetryDelayMs * attempt);
                }
                catch (Exception ex)
                {
                    await _logger.ErrorAsync($"Unexpected error in Redis operation {operationName}", ex);
                    throw;
                }
            }
            throw new InvalidOperationException($"Redis {nameof(operationName)} {operationName} failed after {_redisSettings.MaxRetryAttempts} attempts");
        }

        public Task<bool> HashSetAsync<T>(string hashKey, string key, T value, bool isForceUpdate)
        {
            return ExecuteWithRetryAsync(async () =>
                await redisDatabase.HashSetAsync(hashKey, key, value, nx: !isForceUpdate, flag: CommandFlags.FireAndForget),
                "HashSet");
        }

        public async Task<T?> HashGetAsync<T>(string hashKey, string key)
        {
            return await ExecuteWithRetryAsync(async () =>
                await redisDatabase.HashGetAsync<T>(hashKey ?? string.Empty, key),
                "HashGet");
        }

        public Task<bool> HashExistAsync<T>(string hashKey, string key)
        {
            return ExecuteWithRetryAsync(async () =>
                await redisDatabase.HashExistsAsync(hashKey ?? string.Empty, key),
                "HashExists");
        }

        public Task<bool> UpdateExpiryAsync(string key, DateTimeOffset expiresAt)
        {
            return ExecuteWithRetryAsync(async () =>
                await redisDatabase.UpdateExpiryAsync(key ?? string.Empty, expiresAt, CommandFlags.FireAndForget),
                "UpdateExpiry");
        }

        public Task<long> HashDeleteAsync(string hashKey, string[] keys)
        {
            return ExecuteWithRetryAsync(async () =>
                await redisDatabase.HashDeleteAsync(hashKey ?? string.Empty, keys),
                "HashDelete");
        }

        public async Task HashSetAndExpiryAsync<T>(string hashKey, string key, T value, DateTimeOffset expiresAt, bool isForceUpdate = false)
        {
            await HashSetAsync(hashKey, key, value, isForceUpdate);
            await UpdateExpiryAsync(hashKey, expiresAt);
        }

        public async Task<T> GetAsync<T>(string key)
        {
            return await ExecuteWithRetryAsync(async () =>
                await redisDatabase.GetAsync<T>(key),
                "Get");
        }

        public Task<bool> SetAsync<T>(string key, T value)
        {
            return ExecuteWithRetryAsync(async () =>
                await redisDatabase.SetAddAsync(key, value, flag: CommandFlags.FireAndForget),
                "Set");
        }

        public Task<bool> AddAsyncAndExpiryAsync<T>(string key, T value, DateTimeOffset expiresAt)
        {
            return ExecuteWithRetryAsync(async () =>
                await redisDatabase.AddAsync(key, value, expiresAt, flag: CommandFlags.FireAndForget),
                "AddWithExpiry");
        }

        public Task<long> HashIncrementAsync(string hashKey, string key, long value)
        {
            return ExecuteWithRetryAsync(async () =>
                await redisDatabase.HashIncrementByAsync(hashKey, key, value),
                "HashIncrement");
        }
    }
}
