using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Device.FingerPrint.TransactionAPI.Model
{
    public static class JsonSerializerSettingsFactory
    {
        private static readonly JsonSerializerSettings _instance = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public static JsonSerializerSettings Instance => _instance;
    }
}
