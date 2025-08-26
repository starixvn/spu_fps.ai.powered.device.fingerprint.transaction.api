using Device.FingerPrint.TransactionAPI.Model;
using Fanex.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface ISubscriberService
    {
        Task<string> ValidateAsync(TransactionRequest transactionRequest);
    }

    public class SubscriberService(IAppSettings appSettings, IRedisService redisService, HttpClient httpClient, Fanex.Logging.ILogger logger) : ISubscriberService
    {
        private readonly IAppSettings _appSettings = appSettings;
        private readonly IRedisService _redisService = redisService;
        private readonly HttpClient _httpClient = httpClient;
        private readonly Fanex.Logging.ILogger _logger = logger;

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
            var subscriber = await _redisService.GetAsync<SubscriberModel>(cacheKey);
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
                await _redisService.AddAsyncAndExpiryAsync(cacheKey, subscriber, DateTimeOffset.Now.AddHours(1));
            }
            return subscriber;
        }

        private async Task<SubscriberModel> GetDataSubscriberAsync(string subName)
        {
            try
            {
                var url = $"{_appSettings.GetSubscriberInfo}?subName={subName}";
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
