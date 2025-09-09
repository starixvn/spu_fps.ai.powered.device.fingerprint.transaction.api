using StackExchange.Redis.Extensions.Core.Configuration;

namespace Device.FingerPrint.TransactionAPI.Model
{
    public class RedisSettings : RedisConfiguration
    {
        public List<int> AvailableDatabases { get; set; } = new List<int>();

        public int ShadowDatabase { get; set; } = 10;

        public int TransactionTTLInSeconds { get; set; } = 90;
        public int FingerprintTTLInSeconds { get; set; } = 3 * 60 * 60;
    }
}
