using Device.FingerPrint.TransactionAPI.Model;
using System.ComponentModel;

namespace Device.FingerPrint.TransactionAPI
{
    public interface IAppSettings
    {
        string Environment { get; }
        string QueueName { get; }
        string MobileQueueName { get; }
        bool IsUM { get; }
        string SentryDsn { get; }
        string IgnoredSites { get; }
        string GetSubscriberInfo { get; }
        string FPJSPortialAPI { get; }
        string JWTPassword { get; }
        bool JWTEnable { get; }
        RedisSettings RedisSettings { get; }
    }

    public class AppSettings : IAppSettings
    {
        private readonly IConfiguration configuration;

        public AppSettings(IConfiguration configuration)
        {
            this.configuration = configuration;

            Environment = GetValue<string>("Environment");
            QueueName = GetValue<string>("RabbitMQ:Queue");
            MobileQueueName = GetValue<string>("RabbitMQ:MobileQueue");
            IsUM = GetValue<bool>("IsUM");
            SentryDsn = GetValue<string>("Sentry:Dsn");
            IgnoredSites = GetValue<string>("IgnoredSites");
            GetSubscriberInfo = GetValue<string>("GetSubscriberUrl");
            FPJSPortialAPI = GetValue<string>("FPJS-Portial-API");
            JWTPassword = GetValue<string>("JWT:Password");
            JWTEnable = GetValue<bool>("JWT:Enable");
            RedisSettings = GetSection<RedisSettings>("Redis");
        }

        public string Environment { get; }
        public string QueueName { get; }
        public string MobileQueueName { get; }
        public bool IsUM { get; }
        public string SentryDsn { get; }
        public string IgnoredSites { get; }
        public string GetSubscriberInfo { get; }
        public string FPJSPortialAPI { get; }
        public string JWTPassword { get; }
        public bool JWTEnable { get; }
        public RedisSettings RedisSettings { get; }

        public T GetValue<T>(string key)
        {
            try
            {
                var value = configuration[key];

                if (typeof(T) == typeof(bool))
                {
                    if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        return (T)(object)true;
                    }
                    if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
                    {
                        return (T)(object)false;
                    }
                }

                var typeConverter = TypeDescriptor.GetConverter(typeof(T));
                return (T)typeConverter.ConvertFromString(value);
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Key: {key}", ex);
            }
        }

        public T GetSection<T>(string key)
        {
            var section = configuration.GetSection(key);
            if(section == null)
            {
                return default;
            }
            return section.Get<T>();
        }
    }
}
