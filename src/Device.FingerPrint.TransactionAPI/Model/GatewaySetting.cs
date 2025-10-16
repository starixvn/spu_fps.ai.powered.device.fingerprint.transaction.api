namespace Device.FingerPrint.Model
{
    public class GatewayInfo
    {
        public string? Url { get; set; }

        public string? Username { get; set; }

        public string? Password { get; set; }

        public string? Job { get; set; }

    }

    public interface IGatewaySetting
    {
        GatewayInfo GatewayInfo { get; }

        T GetSection<T>(string key);
    }

    public class GatewaySetting : IGatewaySetting
    {
        private readonly IConfiguration configuration;

        private readonly GatewayInfo gatewayInfo;

        public GatewaySetting(IConfiguration configuration)
        {
            this.configuration = configuration;

            gatewayInfo = GetSection<GatewayInfo>("GatewayConfig") ?? new GatewayInfo();

        }

        public GatewayInfo GatewayInfo => gatewayInfo;

        public T GetSection<T>(string key)
        {
            var schedulerSection = configuration.GetSection(key);

            return schedulerSection.Get<T>();
        }
    }
}
