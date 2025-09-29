using Device.FingerPrint.Model;
using Device.FingerPrint.TransactionAPI.Queue;
using StackExchange.Redis;
using System.Text.Json;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface ILinkingService
    {
        Task<bool> LinkingOnInsert(string sessionKey);

        Task<bool> LinkingOnInsert(TransactionModel transaction, bool requiredFullDetails = false);
    }

    public class LinkingService : ILinkingService
    {
        private readonly IAppSettings _appSettings;
        private readonly IRedisMultiplexDbService _redisService;
        private readonly IMassTransitProducer _massTransitProducer;

        public LinkingService(
            IAppSettings appSettings,
            IRedisMultiplexDbService redisMultiplexDbService,
            IMassTransitProducer massTransitProducer)
        {
            _appSettings = appSettings;
            _redisService = redisMultiplexDbService;
            _massTransitProducer = massTransitProducer;
        }

        public async Task<bool> LinkingOnInsert(string sessionKey)
        {
            var transactionsRedisValue = await _redisService.TransactionDatabase.HashGetAllAsync(sessionKey);

            if (!transactionsRedisValue.Any())
            {
                return false;
            }    

            foreach (var value in transactionsRedisValue)
            {
                if (value.Value.IsNullOrEmpty)
                {
                    continue;
                }

                var transaction = JsonSerializer.Deserialize<TransactionModel>(value.Value);
                if (transaction == null)
                {
                    continue;
                }

                if (await LinkingOnInsert(transaction, requiredFullDetails: true))
                {
                    await _redisService.TransactionDatabase.HashDeleteAsync(transaction.SessionKey, value.Name, CommandFlags.FireAndForget);
                    await _redisService.ShadowDatabase.SortedSetRemoveAsync(_appSettings.RedisSettings.ExpiredTransactionHashKey, $"{transaction.SessionKey}:{transaction.Guid}", CommandFlags.FireAndForget);
                }
            }
            return true;
        }

        public async Task<bool> LinkingOnInsert(TransactionModel transaction, bool requiredFullDetails = false)
        {
            APFingerprintModel? fingerprint4 = null;
            APFingerprintModel? fingerprint5 = null;

            if (!string.IsNullOrEmpty(transaction.DeviceInfo?.F4))
            {
                fingerprint4 = await _redisService.GetAsync<APFingerprintModel?>(_redisService.FP4Database, transaction.DeviceInfo.F4);
                if (fingerprint4 is null && requiredFullDetails)
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(transaction.DeviceInfo?.F5))
            {
                fingerprint5 = await _redisService.GetAsync<APFingerprintModel?>(_redisService.FP5Database, transaction.DeviceInfo.F5);
                if (fingerprint5 is null && requiredFullDetails)
                {
                    return false;
                }
            }

            var messageQueue = BuildMessageQueue(transaction, fingerprint4, fingerprint5);
            await _massTransitProducer.SendAsync(messageQueue, _appSettings.QueueName);

            return true;
        }

        public APFingerprintModel LinkFingerprintsAttributes(APFingerprintModel?[] fingerprints)
        {
            var fingerprintModel = new APFingerprintModel();

            foreach (var fingerprint in fingerprints)
            {
                if (fingerprint is null)
                {
                    continue;
                }

                fingerprintModel.Audio ??= fingerprint.Audio;
                fingerprintModel.Browser ??= fingerprint.Browser;
                fingerprintModel.Graphic ??= fingerprint.Graphic;
                fingerprintModel.Hardware ??= fingerprint.Hardware;
                fingerprintModel.Preferences ??= fingerprint.Preferences;
            }

            return fingerprintModel;
        }

        public MessageQueue BuildMessageQueue(TransactionModel transaction, APFingerprintModel? fingerprint4, APFingerprintModel? fingerprint5)
        {
            var message = new MessageQueue
            {
                Account = transaction.Account,
                Device = transaction.Device,
                Action = transaction.Action,
                ActionResult = transaction.ActionResult,
                Subscriber = transaction.Subscriber,
                SubscriberId = transaction.SubscriberId,
                IP = transaction.Ip,
                FakeIP = transaction.FakeIp,
                TransDate = transaction.TransDate,
                SubscriberDate = DateTimeOffset.Now,
                Token = transaction.Token,
                Recaptcha = "",
                SessionKey = transaction.SessionKey,
                BotD = transaction.Bi,
                Fingerprint = LinkFingerprintsAttributes([fingerprint4, fingerprint5]),
                DeviceInfo = transaction.DeviceInfo,
                IsNewFlow = transaction.IsNewFlow
            };

            return message;
        }
    }
}
