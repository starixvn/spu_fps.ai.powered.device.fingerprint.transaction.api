using Device.FingerPrint.Models;
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
        }

        public async Task InsertTransaction(TransactionModel transaction)
        {
            var pushedToQueue = await _linkingService.LinkingOnInsert(transaction, true);

            if(!pushedToQueue)
            {
                var transactionJson = JsonSerializer.Serialize(transaction);
                await _redisService.TransactionDatabase.SortedSetAddAsync(transaction.SessionKey, transactionJson, transaction.TransDate.Ticks);
                await _redisService.ShadowDatabase.SortedSetAddAsync("TransactionExpireTime", transaction.SessionKey, DateTimeOffset.Now.AddSeconds(_transactionTtlInSeconds).Ticks);
            }

            await _linkingService.LinkingOnInsert(transaction.SessionKey, checkOnExpired: true);
        }
    }
}
