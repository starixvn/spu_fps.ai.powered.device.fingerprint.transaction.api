using StackExchange.Redis.Extensions.Core.Configuration;

namespace Device.FingerPrint.TransactionAPI.Model
{
    public class RedisSettings : RedisConfiguration
    {
        public List<int> AvailableDatabases { get; set; } = new List<int>();

        public int ShadowDatabase { get; set; } = 10;
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
    }
}
