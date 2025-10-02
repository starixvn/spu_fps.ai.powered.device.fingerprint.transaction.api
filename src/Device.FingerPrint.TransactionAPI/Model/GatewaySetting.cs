namespace Device.FingerPrint.Model
{
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
