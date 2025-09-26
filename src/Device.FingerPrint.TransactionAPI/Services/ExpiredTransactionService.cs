using Device.FingerPrint.Models;
using System.Text.Json;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface IExpiredTransactionService
    {
        public Task ScanExpiredTransaction(int pageSize);
    }
    public class ExpiredTransactionService : IExpiredTransactionService
    {
        private readonly IRedisMultiplexDbService _redisService;
        private readonly ILinkingService _linkingService;
        public ExpiredTransactionService(
            IRedisMultiplexDbService redisMultiplexDbService,
            ILinkingService linkingService)
        {
            _redisService = redisMultiplexDbService;
            _linkingService = linkingService;
        }

        public async Task ScanExpiredTransaction(int pageSize) 
        {
            var totalItems = 0;
            while (totalItems < pageSize)
            {
                var redisValue = await _redisService.ShadowDatabase.SortedSetPopAsync("TransactionExpireTime");
                if (redisValue is null)
                {
                    return;
                }

                if(redisValue.Value.Score > DateTimeOffset.Now.Ticks)
                {
                    // Not expired yet, re-add to sorted set
                    await _redisService.ShadowDatabase.SortedSetAddAsync("TransactionExpireTime", redisValue.Value.Element, redisValue.Value.Score);
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

                await _linkingService.LinkingOnInsert(transaction);
            }
        }
    }
}
