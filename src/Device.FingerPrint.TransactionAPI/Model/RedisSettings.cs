namespace Device.FingerPrint.TransactionAPI.Model
{
    public class RedisSettings
    {
        public int ShadowDatabase { get; set; } = 10;
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
    }
}
