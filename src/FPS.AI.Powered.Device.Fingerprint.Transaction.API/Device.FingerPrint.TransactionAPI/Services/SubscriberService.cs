using Device.FingerPrint.TransactionAPI.Model;
using Fanex.Logging;
using System.Text.Json;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface ISubscriberService
    {
        Task<string> ValidateAsync(TransactionRequest transactionRequest);
    }

    public class SubscriberService : ISubscriberService
    {
        private readonly IAppSettings appSettings;
        private readonly IRedisService redisService;

        public SubscriberService(IAppSettings appSettings, IRedisService redisService)
        {
            this.appSettings = appSettings;
            this.redisService = redisService;
        }

        public async Task<string> ValidateAsync(TransactionRequest transactionRequest)
        {
            if (string.IsNullOrEmpty(transactionRequest.Account))
            {
                return TransactionResult.EmptyAccount;
            }

            if (!string.IsNullOrWhiteSpace(transactionRequest.Subscriber))
            {
                var subscriber = await GetSubscriberInfoAsync(transactionRequest.Subscriber);

                if (subscriber != null && subscriber.SubscriberId != 0)
                {
                    transactionRequest.SubscriberId = subscriber.SubscriberId;

                    return TransactionResult.Success;
                }
            }

            return TransactionResult.NotExistSubscriber;
        }

        private async Task<SubscriberModel> GetSubscriberInfoAsync(string subName)
        {

            if (!string.IsNullOrWhiteSpace(subName))
            {
                var cacheKey = $"TransAPI:{subName}";

                SubscriberModel subInfo = await GetSubscriberInfoByCache(cacheKey, subName);

                if (subInfo != null && subInfo.SubscriberId != 0)
                {
                    return subInfo;
                }

                return await GetDataSubscriberAndSetCacheAsync(subName, cacheKey);

            }

            return new SubscriberModel();
        }

        private async Task<SubscriberModel> GetSubscriberInfoByCache(string cacheKey, string subName)
        {
            try
            {
                return await redisService.GetAsync<SubscriberModel>(cacheKey);
            }
            catch (Exception)
            {
                return await GetDataSubscriberAndSetCacheAsync(subName, cacheKey);
            }
        }

        private async Task<SubscriberModel> GetDataSubscriberAndSetCacheAsync(string subName, string cacheKey)
        {
            var subData = await GetDataSubscriberAsync(subName);

            if (subData != null && subData.SubscriberId != 0)
            {
                _ = redisService.AddAsyncAndExpiryAsync(cacheKey, subData, DateTimeOffset.UtcNow.AddMinutes(120));
            }

            return subData;
        }

        private async Task<SubscriberModel> GetDataSubscriberAsync(string subName)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(1);
                var url = $"{appSettings.GetSubscriberInfo}?subscriberName={subName}";

                var result = await httpClient.GetAsync(url)
                    .ContinueWith((responseTask) =>
                    {
                        var response = responseTask.Result;
                        var jsonString = response.Content.ReadAsStringAsync();

                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };

                        return JsonSerializer.Deserialize<SubscriberModel>(jsonString.Result, options);
                    });

                return result ?? new SubscriberModel();
            }
            catch
            {
                LogService.Error($"Cannot getSubscriber {subName} from {appSettings.GetSubscriberInfo}");
                return new SubscriberModel();
            }
        }
    }
}
