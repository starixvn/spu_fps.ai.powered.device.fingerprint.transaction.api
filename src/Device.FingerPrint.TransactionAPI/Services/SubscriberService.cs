using Device.FingerPrint.Models;
using Newtonsoft.Json;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface ISubscriberService
    {
        Task<string> ValidateAsync(TransactionRequest transactionRequest);
    }

    public class SubscriberService(IAppSettings appSettings, IRedisMultiplexDbService redisMultiplexDbService, HttpClient httpClient, ILogger logger) : ISubscriberService
    {
        private readonly IAppSettings _appSettings = appSettings;
        private readonly IRedisMultiplexDbService _redisService = redisMultiplexDbService;
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger _logger = logger;

        public async Task<string> ValidateAsync(TransactionRequest transactionRequest)
        {
            if (string.IsNullOrEmpty(transactionRequest.Account))
            {
                return TransactionResult.EmptyAccount;
            }

            if (string.IsNullOrWhiteSpace(transactionRequest.Subscriber))
            {
                return TransactionResult.NotExistSubscriber;
            }

            var subscriber = await GetSubscriberInfoAsync(transactionRequest.Subscriber);

            if (subscriber == null || subscriber.SubscriberId == 0)
            {
                return TransactionResult.NotExistSubscriber;
            }

            transactionRequest.SubscriberId = subscriber.SubscriberId;
            return TransactionResult.Success;
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
