using Device.FingerPrint.Model;
using System.Text.Json;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface ITransactionService
    {
        Task InsertTransaction(TransactionModel transaction);
    }

    public class TransactionService : ITransactionService
    {
        private readonly int _transactionTtlInSeconds;
        private readonly string _expiredTransactionHashKey;
        private readonly IRedisMultiplexDbService _redisService;
        private readonly ILinkingService _linkingService;

        public TransactionService(
            IAppSettings appSettings,
            IRedisMultiplexDbService redisService,
            ILinkingService linkingService)
        {
            _redisService = redisService;
            _linkingService = linkingService;

            _transactionTtlInSeconds = appSettings.RedisSettings.TransactionTTLInSeconds;
            _expiredTransactionHashKey = appSettings.RedisSettings.ExpiredTransactionHashKey;
        }

        public async Task InsertTransaction(TransactionModel transaction)
        {
            var pushedToQueue = await _linkingService.LinkingOnInsert(transaction, "Transaction", true);

            if(!pushedToQueue)
            {
                var transactionJson = JsonSerializer.Serialize(transaction);
                await _redisService.TransactionDatabase.HashSetAsync(transaction.SessionKey, transaction.Guid.ToString(), transactionJson);

                var key = $"{transaction.SessionKey}:{transaction.Guid}";
                var expireTime = DateTimeOffset.Now.AddSeconds(_transactionTtlInSeconds).Ticks;
                await _redisService.ShadowDatabase.SortedSetAddAsync(_expiredTransactionHashKey, key, expireTime);
            }
        }
    }
}
