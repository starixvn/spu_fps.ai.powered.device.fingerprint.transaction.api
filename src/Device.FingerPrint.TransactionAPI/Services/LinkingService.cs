using Device.FingerPrint.TransactionAPI.Model;
using Device.FingerPrint.TransactionAPI.Queue;
using System.Globalization;
using System.Text.Json;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface ILinkingService
    {
        string FP4_REDIS_KEY { get; }
        string FP5_REDIS_KEY { get; }
        string TRANSACTION_REDIS_KEY { get; }

        Task LinkingOnInsert(string sessionKey);

        Task InsertFingerprint(string sessionKey, string key, APFingerprintModel fingerprint);
        Task InsertTransaction(string sessionKey, TransactionRequest transaction);
    }

    public class LinkingService : ILinkingService
    {
        public string FP4_REDIS_KEY { get; } = "FP4";
        public string FP5_REDIS_KEY { get; } = "FP5";
        public string TRANSACTION_REDIS_KEY { get; } = "Transaction";

        private const int TRANSACTION_TTL_IN_SECONDS = 90;

        private const int FINGERPRINT_TTL_IN_SECONDS = 3 * 60 * 60;

        private readonly ILogger _logger;
        private readonly IAppSettings _appSettings;
        private readonly IRedisService _redisService;
        private readonly IMassTransitProducer _massTransitProducer;

        public LinkingService(ILogger logger, IAppSettings appSettings, IRedisService redisService, IMassTransitProducer massTransitProducer)
        {
            _logger = logger;
            _appSettings = appSettings;
            _redisService = redisService;
            _massTransitProducer = massTransitProducer;
        }

        public async Task InsertFingerprint(string sessionKey, string key, APFingerprintModel fingerprint)
        {
            var updateExpire = true;

            var isTransactionExists = await _redisService.HashExistAsync<string>(sessionKey, TRANSACTION_REDIS_KEY);
            var isFingerprintExists = await _redisService.HashExistAsync<string>(sessionKey, (key == FP4_REDIS_KEY ? FP5_REDIS_KEY : FP4_REDIS_KEY));
            if (isTransactionExists || isFingerprintExists)
            {
                updateExpire = false;
            }

            if(updateExpire)
            {
                var expireAt = DateTimeOffset.Now.AddSeconds(FINGERPRINT_TTL_IN_SECONDS);
                var fingerprintJson = JsonSerializer.Serialize(fingerprint);

                await _redisService.HashSetAndExpiryAsync(sessionKey, key, fingerprintJson, expireAt, isForceUpdate: true);
            }
            else
            {
                var fingerprintJson = JsonSerializer.Serialize(fingerprint);
                await _redisService.HashSetAsync(sessionKey, key, fingerprintJson, isForceUpdate: true);
            }
        }

        public async Task InsertTransaction(string sessionKey, TransactionRequest transaction)
        {
            var updateExpire = true;

            var isTransactionExists = await _redisService.HashExistAsync<string>(sessionKey, TRANSACTION_REDIS_KEY);
            if (isTransactionExists)
            {
                updateExpire = false;
            }

            if (updateExpire)
            {
                var expireAt = DateTimeOffset.Now.AddSeconds(TRANSACTION_TTL_IN_SECONDS);
                var transactionJson = JsonSerializer.Serialize(transaction);

                await _redisService.HashSetAndExpiryAsync(sessionKey, TRANSACTION_REDIS_KEY, transactionJson, expireAt, isForceUpdate: true);
            }
            else
            {
                var transactionJson = JsonSerializer.Serialize(transaction);
                await _redisService.HashSetAsync(sessionKey, TRANSACTION_REDIS_KEY, transactionJson, isForceUpdate: true);
            }
        }

        public async Task LinkingOnInsert(string sessionKey)
        {
            try
            {
                if (!await _redisService.HashExistAsync<string>(sessionKey, FP4_REDIS_KEY))
                {
                    return;
                }

                if (!await _redisService.HashExistAsync<string>(sessionKey, FP5_REDIS_KEY))
                {
                    return;
                }

                if (!await _redisService.HashExistAsync<string>(sessionKey, TRANSACTION_REDIS_KEY))
                {
                    return;
                }

                var fingerprint4RedisValue = await _redisService.HashGetAsync<string>(sessionKey, FP4_REDIS_KEY);
                var fingerprint5RedisValue = await _redisService.HashGetAsync<string>(sessionKey, FP5_REDIS_KEY);
                var transactionRedisValue = await _redisService.HashGetAsync<string>(sessionKey, TRANSACTION_REDIS_KEY);

                var fingerprint4 = JsonSerializer.Deserialize<APFingerprintModel>(fingerprint4RedisValue);
                var fingerprint5 = JsonSerializer.Deserialize<APFingerprintModel>(fingerprint5RedisValue);
                var transactionRequest = JsonSerializer.Deserialize<TransactionRequest>(transactionRedisValue);

                var messageQueue = BuildMessageQueue(transactionRequest, fingerprint4, fingerprint5);

                await _massTransitProducer.SendAsync(messageQueue, _appSettings.QueueName);

                await _redisService.HashDeleteAsync(sessionKey, [FP4_REDIS_KEY, FP5_REDIS_KEY, TRANSACTION_REDIS_KEY]);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error linking session {sessionKey}", ex);
                throw;
            }
        }

        public APFingerprintModel LinkFingerprintsAttributes(APFingerprintModel?[] fingerprints)
        {
            var fingerprintModel = new APFingerprintModel();

            foreach(var fingerprint in fingerprints)
            {
                if (fingerprint is null)
                {
                    continue;
                }

                fingerprintModel.Audio ??= fingerprint.Audio;
                fingerprintModel.Browser ??= fingerprint.Browser;
                fingerprintModel.Graphics ??= fingerprint.Graphics;
                fingerprintModel.Hardware ??= fingerprint.Hardware;
                fingerprintModel.Preferences ??= fingerprint.Preferences;
            }
               
            return fingerprintModel;
        }

        public DeviceModel BuildDeviceModel(TransactionRequest transactionRequest)
        {
            return new DeviceModel
            {
                AcceptsTypes = transactionRequest.AcceptsTypes,
                DetecasAnalysis = transactionRequest.DetecasAnalysis,
                Di = transactionRequest.Di ?? transactionRequest.__di,
                Ip = transactionRequest.Ip,
                Url = transactionRequest.Url,
                Platform = transactionRequest.Platform,
                Screen = transactionRequest.Screen,
                TotalMinutes = (DateTimeOffset.UtcNow - DateTimeOffset.Now).TotalMinutes.ToString(CultureInfo.InvariantCulture),
                UserAgent = transactionRequest.UserAgent
            };

        }

        public MessageQueue BuildMessageQueue(TransactionRequest transactionRequest, APFingerprintModel? fingerprint4, APFingerprintModel? fingerprint5)
        {
            return new MessageQueue
            {
                Account = transactionRequest.Account,
                Device = BuildDeviceModel(transactionRequest),
                Action = transactionRequest.Action,
                ActionResult = transactionRequest.ActionResult,
                Subscriber = transactionRequest.Subscriber,
                SubscriberId = transactionRequest.SubscriberId,
                IP = transactionRequest.Ip,
                FakeIP = transactionRequest.FakeIp,
                TransDate = transactionRequest.TransDate,
                SubscriberDate = DateTimeOffset.Now,
                Token = transactionRequest.Token,
                Recaptcha = "",
                SessionKey = transactionRequest.SessionKey,
                BotD = transactionRequest.Bi,
                FingerPrint = LinkFingerprintsAttributes([fingerprint4, fingerprint5])
            };

        }
    }
}
