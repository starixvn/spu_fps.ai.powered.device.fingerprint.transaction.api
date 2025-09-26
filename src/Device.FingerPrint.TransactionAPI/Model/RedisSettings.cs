using StackExchange.Redis.Extensions.Core.Configuration;

namespace Device.FingerPrint.Models
{
    public class RedisSettings : RedisConfiguration
    {
        public int ShadowDatabase { get; set; } = 10;
        public int FP4Database { get; set; } = 10;
        public int FP5Database { get; set; } = 10;
        public int TransactionDatabase { get; set; } = 10;

        public int TransactionTTLInSeconds { get; set; } = 90;
        public int FingerprintTTLInSeconds { get; set; } = 3 * 60 * 60;
    }
}
