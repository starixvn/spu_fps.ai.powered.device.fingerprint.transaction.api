using Device.FingerPrint.Models;
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

                var sessionKey = redisValue.Value.Element.ToString();

                var transactionsRedisValue = await _redisService.TransactionDatabase.SortedSetRangeByRankAsync(sessionKey);

                if (!transactionsRedisValue.Any())
                {
                    continue;
                }

                var transactions = transactionsRedisValue
                    .Select(value => JsonSerializer.Deserialize<TransactionModel>(value))
                    .Where(tr => tr != null)
                    .ToList();
                foreach (var transaction in transactions)
                {
                    if (transaction == null)
                    {
                        continue;
                    }

                    await _linkingService.LinkingOnInsert(transaction, requiredFullDetails: false);
                }
            }
        }
    }
}
