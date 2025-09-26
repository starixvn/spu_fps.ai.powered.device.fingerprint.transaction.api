using StackExchange.Redis;
using System.Text.Json;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface IRedisMultiplexDbService
    {
        IDatabase DefaultDatabase { get; }

        IDatabase ShadowDatabase { get; }

        IDatabase FP4Database { get; }

        IDatabase FP5Database { get; }

        IDatabase TransactionDatabase { get; }

        Task<bool> SetAndExpireAsync<T>(IDatabase db, string hashKey, T value, long expireInSecond);

        Task<T> GetAsync<T>(IDatabase db, string hashKey);
    }

    public class RedisMultiplexDbService : IRedisMultiplexDbService
    {
        private readonly IDatabase defaultDatabase;
        private readonly IDatabase shadowDatabase;
        private readonly IDatabase fp4Database;
        private readonly IDatabase fp5Database;
        private readonly IDatabase transactionDatabase;

        public RedisMultiplexDbService(IConfiguration configuration, IAppSettings appSettings)
        {
            var redisSettings = appSettings.RedisSettings;
            var connectionMultiplexer = ConnectionMultiplexer.Connect(redisSettings.ConfigurationOptions);

            defaultDatabase = connectionMultiplexer.GetDatabase();
            shadowDatabase = connectionMultiplexer.GetDatabase(redisSettings.ShadowDatabase);
            fp4Database = connectionMultiplexer.GetDatabase(redisSettings.FP4Database);
            fp5Database = connectionMultiplexer.GetDatabase(redisSettings.FP5Database);
            transactionDatabase = connectionMultiplexer.GetDatabase(redisSettings.TransactionDatabase);
            
        }
        public IDatabase DefaultDatabase => defaultDatabase;

        public IDatabase ShadowDatabase => shadowDatabase;

        public IDatabase FP4Database => fp4Database;

        public IDatabase FP5Database => fp5Database;

        public IDatabase TransactionDatabase => transactionDatabase;

        public async Task<bool> SetAndExpireAsync<T>(IDatabase db, string hashKey, T value, long expireInSecond)
        {
            var isSet = await db.StringSetAsync(hashKey, JsonSerializer.Serialize(value));
            if (isSet)
            {
                return await db.KeyExpireAsync(hashKey, TimeSpan.FromSeconds(expireInSecond), CommandFlags.FireAndForget);
            }
            return false;
        }

        public async Task<T> GetAsync<T>(IDatabase db, string hashKey)
        {
            var value = await db.StringGetAsync(hashKey);

            if (value.IsNullOrEmpty)
            {
                return default(T);
            }

            return JsonSerializer.Deserialize<T>(value);
        }
    }
}
