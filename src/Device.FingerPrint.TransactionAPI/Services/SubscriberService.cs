using Device.FingerPrint.Model;
using Newtonsoft.Json;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface ISubscriberService
    {
        Task<bool> ValidateAsync(string subscriber);
    }

    public class SubscriberService(IAppSettings appSettings, IRedisMultiplexDbService redisMultiplexDbService, HttpClient httpClient, ILogger logger) : ISubscriberService
    {
        private readonly IAppSettings _appSettings = appSettings;
        private readonly IRedisMultiplexDbService _redisService = redisMultiplexDbService;
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger _logger = logger;

        public async Task<bool> ValidateAsync(string subscriber)
        {
            if (!string.IsNullOrWhiteSpace(subscriber))
            {
                var subscriberInfo = await GetSubscriberInfoAsync(subscriber);

                if (subscriberInfo != null && subscriberInfo.SubscriberId != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<SubscriberModel> GetSubscriberInfoAsync(string subName)
        {
            var cacheKey = $"TransAPI:{subName}";
            try
            {
                return await GetSubscriberInfoByCache(cacheKey, subName);
            }
            catch (Exception ex)
            {
                _logger.Error($"Get sub info from cache has ex: {ex.Message}", ex);
                return await GetDataSubscriberAsync(subName);
            }
        }

        private async Task<SubscriberModel> GetSubscriberInfoByCache(string cacheKey, string subName)
        {
            var subscriber = await _redisService.GetAsync<SubscriberModel>(_redisService.DefaultDatabase, cacheKey);
            if (subscriber != null)
            {
                return subscriber;
            }

            return await GetDataSubscriberAndSetCacheAsync(subName, cacheKey);
        }

        private async Task<SubscriberModel> GetDataSubscriberAndSetCacheAsync(string subName, string cacheKey)
        {
            var subscriber = await GetDataSubscriberAsync(subName);
            if (subscriber != null)
            {
                await _redisService.SetAndExpireAsync(_redisService.DefaultDatabase, cacheKey, subscriber, 3600);
            }
            return subscriber;
        }

        private async Task<SubscriberModel> GetDataSubscriberAsync(string subName)
        {
            try
            {
                var url = $"{_appSettings.GetSubscriberInfo}?subscriberName={subName}";
                var response = await _httpClient.GetStringAsync(url);
                return JsonConvert.DeserializeObject<SubscriberModel>(response);
            }
            catch (Exception ex)
            {
                _logger.Error($"Get subscriber info from API has ex: {ex.Message}", ex);
                return null;
            }
        }
    }
}
