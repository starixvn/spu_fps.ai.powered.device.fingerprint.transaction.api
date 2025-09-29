using Device.FingerPrint.Model;
using StackExchange.Redis;
using System.Text.Json;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface IExpiredTransactionService
    {
        public Task ScanExpiredTransaction(int pageSize);
    }
    public class ExpiredTransactionService : IExpiredTransactionService
    {
        private readonly IAppSettings _appSettings;
        private readonly IRedisMultiplexDbService _redisService;
        private readonly ILinkingService _linkingService;
        public ExpiredTransactionService(
            IAppSettings appSettings,
            IRedisMultiplexDbService redisMultiplexDbService,
            ILinkingService linkingService)
        {
            _appSettings = appSettings;
            _redisService = redisMultiplexDbService;
            _linkingService = linkingService;
        }

        public async Task ScanExpiredTransaction(int pageSize) 
        {
            var totalItems = 0;
            while (totalItems < pageSize)
            {
                var redisValue = await _redisService.ShadowDatabase.SortedSetPopAsync(_appSettings.RedisSettings.ExpiredTransactionHashKey);
                if (redisValue is null)
                {
                    return;
                }

                if(redisValue.Value.Score > DateTimeOffset.Now.Ticks)
                {
                    // Not expired yet, re-add to sorted set
                    await _redisService.ShadowDatabase.SortedSetAddAsync(_appSettings.RedisSettings.ExpiredTransactionHashKey, redisValue.Value.Element, redisValue.Value.Score);
                    return;
                }

                totalItems++;

                var key = redisValue.Value.Element.ToString();
                var sessionKey = key.Substring(0, key.LastIndexOf(':'));
                var guid = key.Substring(key.LastIndexOf(':') + 1);

                var transactionsRedisValue = await _redisService.TransactionDatabase.HashGetAsync(sessionKey, guid);

                if (transactionsRedisValue.IsNullOrEmpty)
                {
                    continue;
                }

                var transaction = JsonSerializer.Deserialize<TransactionModel>(transactionsRedisValue);
                if (transaction == null)
                {
                    continue;
                }

                if(await _linkingService.LinkingOnInsert(transaction))
                {
                    await _redisService.TransactionDatabase.HashDeleteAsync(sessionKey, guid, CommandFlags.FireAndForget);
                }
            }
        }
    }
}
