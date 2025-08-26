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
    }

    public class AppSettings : IAppSettings
    {
        private readonly IConfiguration configuration;

        private readonly string environment;

        private readonly string queueName;

        private readonly string mobileQueueName;

        private readonly bool isUM;

        private readonly string sentryDsn;

        private readonly string ignoredSites;

        private readonly string getSubscriberInfo;

        private readonly string fPJSPortialAPI;

        private readonly string jwtPassword;

        private readonly bool jwtEnable;

        public AppSettings(IConfiguration configuration)
        {
            this.configuration = configuration;

            environment = GetValue<string>("Environment");

            queueName = GetValue<string>("RabbitMQ:Queue");

            mobileQueueName = GetValue<string>("RabbitMQ:MobileQueue");

            isUM = GetValue<bool>("IsUM");

            sentryDsn = GetValue<string>("Sentry:Dsn");

            ignoredSites = GetValue<string>("IgnoredSites");

            getSubscriberInfo = GetValue<string>("GetSubscriberUrl");

            fPJSPortialAPI = GetValue<string>("FPJS-Portial-API");

            jwtPassword = GetValue<string>("JWT:Password");

            jwtEnable = GetValue<bool>("JWT:Enable");
        }

        public string Environment => environment;

        public string QueueName => queueName;

        public string MobileQueueName => mobileQueueName;

        public bool IsUM => isUM;

        public string SentryDsn => sentryDsn;

        public string IgnoredSites => ignoredSites;

        public string GetSubscriberInfo => getSubscriberInfo;

        public string FPJSPortialAPI => fPJSPortialAPI;

        public string JWTPassword => jwtPassword;

        public bool JWTEnable => jwtEnable;

        public T GetValue<T>(string key)
        {
            try
            {
                var value = configuration[key];

                // Handle boolean conversion with extended support for "1"/"0"
                if (typeof(T) == typeof(bool))
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        throw new InvalidCastException($"Key: {key} - Value cannot be null or empty for boolean conversion");
                    }

                    // Support standard boolean strings and numeric representations
                    switch (value.ToLowerInvariant())
                    {
                        case "true":
                        case "1":
                            return (T)(object)true;
                        case "false":
                        case "0":
                            return (T)(object)false;
                        default:
                            // Try standard boolean parsing for other cases
                            if (bool.TryParse(value, out bool boolResult))
                            {
                                return (T)(object)boolResult;
                            }
                            throw new FormatException($"'{value}' is not a valid boolean value. Expected: true, false, 1, or 0");
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
    }
}
