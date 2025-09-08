using Device.FingerPrint.TransactionAPI.Model;
using StackExchange.Redis;
using System.Text.Json;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface IRedisMultiplexDbService
    {
        IDatabase DefaultDatabase { get; }

        IDatabase ShadowDatabase { get; }

        IDatabase GetDatabase(int db = -1); 

        IServer GetServer(string host, int port);

        ISubscriber GetSubscriber();

        IConnectionMultiplexer GetConnection();

        Task<int> GetDbIndex(string hashedKey);

        Task<bool> HashSetAndExpireAsync<T>(IDatabase db, string hashKey, T value, long expireInSecond);

        Task HashSetAsync<T>(IDatabase db, string hashKey, string key, T value, bool isForceUpdate);

        Task<bool> HashExistAsync<T>(IDatabase db, string hashKey, string key);

        Task<T> HashGetAsync<T>(IDatabase db, string hashKey);

        Task HashDeleteAsync(IDatabase db, string hashKey);
    }

    public class RedisMultiplexDbService : IRedisMultiplexDbService
    {
        private static int _nextDbIndex;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly RedisSettings _redisSettings;

        private readonly IDatabase defaultDatabase;
        private readonly IDatabase shadowDatabase;

        public RedisMultiplexDbService(IConfiguration configuration, IAppSettings appSettings)
        {
            _redisSettings = appSettings.RedisSettings;
            _connectionMultiplexer = ConnectionMultiplexer.Connect(_redisSettings.ConfigurationOptions);

            defaultDatabase = _connectionMultiplexer.GetDatabase();
            shadowDatabase = _connectionMultiplexer.GetDatabase(_redisSettings.ShadowDatabase);
            
        }
        public IDatabase DefaultDatabase => defaultDatabase;

        public IDatabase ShadowDatabase => shadowDatabase;

        public IConnectionMultiplexer GetConnection()
        {
            return _connectionMultiplexer;
        }

        public IDatabase GetDatabase(int db = -1)
        {
            return _connectionMultiplexer.GetDatabase(db);
        }

        public IServer GetServer(string host, int port)
        {
            return _connectionMultiplexer.GetServer(host, port);
        }

        public ISubscriber GetSubscriber()
        {
            return _connectionMultiplexer.GetSubscriber();
        }

        public async Task<int> GetDbIndex(string hashedKey)
        {
            var dbIndex = await shadowDatabase.StringGetAsync(hashedKey);
            if (!dbIndex.IsNullOrEmpty
                && int.TryParse(dbIndex.ToString(), out int dbIndexValue))
            {
                return dbIndexValue;
            }

            return _redisSettings.AvailableDatabases[
                Interlocked.Increment(ref _nextDbIndex) % _redisSettings.AvailableDatabases.Count];
        }

        public async Task<bool> HashSetAndExpireAsync<T>(IDatabase db, string hashKey, T value, long expireInSecond)
        {
            var isSet = await db.StringSetAsync(hashKey, JsonSerializer.Serialize(value));
            if (isSet)
            {
                return await db.KeyExpireAsync(hashKey, TimeSpan.FromSeconds(expireInSecond), CommandFlags.FireAndForget);
            }
            return false;
        }

        public async Task HashSetAsync<T>(IDatabase db, string hashKey, string key, T value, bool isForceUpdate)
        {
            await db.HashSetAsync(hashKey, key, JsonSerializer.Serialize(value), when: isForceUpdate ? When.Always : When.NotExists);
        }

        public Task<bool> HashExistAsync<T>(IDatabase db, string hashKey, string key)
        {
            return db.HashExistsAsync(hashKey, key);
        }

        public async Task<T> HashGetAsync<T>(IDatabase db, string hashKey)
        {
            var value = await db.StringGetAsync(hashKey);

            return JsonSerializer.Deserialize<T>(value);
        }

        public async Task HashDeleteAsync(IDatabase db, string hashKey)
        {
            await db.KeyDeleteAsync(hashKey);
        }
    }
}
