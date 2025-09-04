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

        Task<bool> LinkingOnInsert(string sessionKey);
        Task<bool> LinkingOnExpired(string key);

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
            var shaddowKey = $"{sessionKey}_shadow";

            var isTransactionExists = await _redisService.HashExistAsync<string>(sessionKey, TRANSACTION_REDIS_KEY);
            var isFingerprintExists = await _redisService.HashExistAsync<string>(sessionKey, (key == FP4_REDIS_KEY ? FP5_REDIS_KEY : FP4_REDIS_KEY));
            if (isTransactionExists || isFingerprintExists)
            {
                updateExpire = false;
            }

            if(updateExpire)
            {
                var expireAt = DateTimeOffset.Now.AddSeconds(FINGERPRINT_TTL_IN_SECONDS);
                await _redisService.AddAsyncAndExpiryAsync(shaddowKey, "1", expireAt);
            }

            var fingerprintJson = JsonSerializer.Serialize(fingerprint);
            await _redisService.HashSetAsync(sessionKey, key, fingerprintJson, isForceUpdate: true);

            await LinkingOnInsert(sessionKey);
        }

        public async Task InsertTransaction(string sessionKey, TransactionRequest transaction)
        {
            var updateExpire = true;
            var shaddowKey = $"{sessionKey}_shadow";
            var isTransactionExists = await _redisService.HashExistAsync<string>(sessionKey, TRANSACTION_REDIS_KEY);
            if (isTransactionExists)
            {
                updateExpire = false;
            }

            if (updateExpire)
            {
                var expireAt = DateTimeOffset.Now.AddSeconds(TRANSACTION_TTL_IN_SECONDS);
                await _redisService.AddAsyncAndExpiryAsync(shaddowKey, "1", expireAt);
            }

            var transactionJson = JsonSerializer.Serialize(transaction);
            await _redisService.HashSetAsync(sessionKey, TRANSACTION_REDIS_KEY, transactionJson, isForceUpdate: true);

            await LinkingOnInsert(sessionKey);
        }

        public async Task<bool> LinkingOnInsert(string sessionKey)
        {
            try
            {
                var batch = _redisService.RedisDatabase.Database.CreateBatch();

                var getFP4Task = batch.HashGetAsync(sessionKey, FP4_REDIS_KEY);
                var getFP5Task = batch.HashGetAsync(sessionKey, FP5_REDIS_KEY);
                var getTransactionTask = batch.HashGetAsync(sessionKey, TRANSACTION_REDIS_KEY);

                batch.Execute();
                await Task.WhenAll(getFP4Task, getFP5Task, getTransactionTask);

                var fingerprint4RedisValue = await getFP4Task;
                var fingerprint5RedisValue = await getFP5Task;
                var transactionRedisValue = await getTransactionTask;

                if (fingerprint4RedisValue.IsNullOrEmpty
                    || fingerprint5RedisValue.IsNullOrEmpty
                    || transactionRedisValue.IsNullOrEmpty)
                {
                    return false;
                }

                var fingerprint4 = JsonSerializer.Deserialize<APFingerprintModel>(fingerprint4RedisValue);
                var fingerprint5 = JsonSerializer.Deserialize<APFingerprintModel>(fingerprint5RedisValue);
                var transactionRequest = JsonSerializer.Deserialize<TransactionRequest>(transactionRedisValue);

                var messageQueue = BuildMessageQueue(transactionRequest, fingerprint4, fingerprint5);

                await _massTransitProducer.SendAsync(messageQueue, _appSettings.QueueName);

                await _redisService.HashDeleteAsync(sessionKey, [FP4_REDIS_KEY, FP5_REDIS_KEY, TRANSACTION_REDIS_KEY]);
                return true;
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error linking session {sessionKey}", ex);
                throw;
            }
        }

        public async Task<bool> LinkingOnExpired(string key)
        {
            var sessionKey = key.Replace("_shadow", "");

            try
            {
                var batch = _redisService.RedisDatabase.Database.CreateBatch();

                var getFP4Task = batch.HashGetAsync(sessionKey, FP4_REDIS_KEY);
                var getFP5Task = batch.HashGetAsync(sessionKey, FP5_REDIS_KEY);
                var getTransactionTask = batch.HashGetAsync(sessionKey, TRANSACTION_REDIS_KEY);

                batch.Execute();
                await Task.WhenAll(getFP4Task, getFP5Task, getTransactionTask);

                var fingerprint4RedisValue = await getFP4Task;
                var fingerprint5RedisValue = await getFP5Task;
                var transactionRedisValue = await getTransactionTask;

                if (transactionRedisValue.IsNullOrEmpty)
                {
                    await _redisService.HashDeleteAsync(sessionKey, [FP4_REDIS_KEY, FP5_REDIS_KEY, TRANSACTION_REDIS_KEY]);
                    return false;
                }

                var fingerprint4 = new APFingerprintModel();
                if (!fingerprint4RedisValue.IsNullOrEmpty)
                {
                    fingerprint4 = JsonSerializer.Deserialize<APFingerprintModel>(fingerprint4RedisValue);
                }

                var fingerprint5 = new APFingerprintModel();
                if (!fingerprint5RedisValue.IsNullOrEmpty)
                {
                    fingerprint5 = JsonSerializer.Deserialize<APFingerprintModel>(fingerprint5RedisValue);
                }

                var transactionRequest = JsonSerializer.Deserialize<TransactionRequest>(transactionRedisValue);

                var messageQueue = BuildMessageQueue(transactionRequest, fingerprint4, fingerprint5);

                await _massTransitProducer.SendAsync(messageQueue, _appSettings.QueueName);

                await _redisService.HashDeleteAsync(sessionKey, [FP4_REDIS_KEY, FP5_REDIS_KEY, TRANSACTION_REDIS_KEY]);
                return true;
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
