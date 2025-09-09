using Device.FingerPrint.TransactionAPI.Model;
using Device.FingerPrint.TransactionAPI.Queue;
using StackExchange.Redis;
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
        Task<bool> LinkingOnExpired(string hashkey);

        Task<string> InsertFingerprint(string sessionKey, string key, APFingerprintModel fingerprint);
        Task<string> InsertTransaction(string sessionKey, TransactionRequest transaction);
    }

    public class LinkingService : ILinkingService
    {
        public string FP4_REDIS_KEY { get; } = "FP4";
        public string FP5_REDIS_KEY { get; } = "FP5";
        public string TRANSACTION_REDIS_KEY { get; } = "Transaction";

        private const int TRANSACTION_TTL_IN_SECONDS = 90;

        private const int FINGERPRINT_TTL_IN_SECONDS = 3 * 60 * 60;

        private readonly IAppSettings _appSettings;
        private readonly IRedisMultiplexDbService _redisService;
        private readonly IMassTransitProducer _massTransitProducer;

        public LinkingService(IAppSettings appSettings, IRedisMultiplexDbService redisMultiplexDbService, IMassTransitProducer massTransitProducer)
        {
            _appSettings = appSettings;
            _redisService = redisMultiplexDbService;
            _massTransitProducer = massTransitProducer;
        }

        public async Task<string> InsertFingerprint(string sessionKey, string key, APFingerprintModel fingerprint)
        {
            var updateExpire = true;
            var dbIndex = await _redisService.GetDbIndex(sessionKey);
            var redisDb = _redisService.GetDatabase(dbIndex);

            var isTransactionExists = await _redisService.HashExistAsync<string>(redisDb, sessionKey, TRANSACTION_REDIS_KEY);
            var isFingerprintExists = await _redisService.HashExistAsync<string>(redisDb, sessionKey, (key == FP4_REDIS_KEY ? FP5_REDIS_KEY : FP4_REDIS_KEY));
            if (isTransactionExists || isFingerprintExists)
            {
                updateExpire = false;
            }

            await InsertRedis(redisDb, dbIndex, sessionKey, key, fingerprint, FINGERPRINT_TTL_IN_SECONDS, updateExpire);

            await LinkingOnInsert(sessionKey);
            return $"{sessionKey}:{dbIndex}";
        }

        public async Task<string> InsertTransaction(string sessionKey, TransactionRequest transaction)
        {
            var updateExpire = true;
            var dbIndex = await _redisService.GetDbIndex(sessionKey);
            var redisDb = _redisService.GetDatabase(dbIndex);

            var isTransactionExists = await _redisService.HashExistAsync<string>(redisDb, sessionKey, TRANSACTION_REDIS_KEY);
            if (isTransactionExists)
            {
                updateExpire = false;
            }

            await InsertRedis(redisDb, dbIndex, sessionKey, TRANSACTION_REDIS_KEY, transaction, TRANSACTION_TTL_IN_SECONDS, updateExpire);

            await LinkingOnInsert(sessionKey);

            return $"{sessionKey}:{dbIndex}";
        }

        public async Task<bool> LinkingOnInsert(string sessionKey)
        {
            var dbIndex = await _redisService.GetDbIndex(sessionKey);
            var redisDb = _redisService.GetDatabase(dbIndex);

            var batch = redisDb.CreateBatch();

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
            var transactionRequest = JsonSerializer.Deserialize<TransactionRequest>(transactionRedisValue.ToString());

            var messageQueue = BuildMessageQueue(transactionRequest, fingerprint4, fingerprint5);

            await _massTransitProducer.SendAsync(messageQueue, _appSettings.QueueName);

            await _redisService.HashDeleteAsync(redisDb, sessionKey);
            await _redisService.ShadowDatabase.KeyDeleteAsync($"{sessionKey}:{dbIndex}", CommandFlags.FireAndForget);

            return true;
        }

        public async Task<bool> LinkingOnExpired(string hashkey)
        {
            if ( string.IsNullOrEmpty(hashkey) || !hashkey.Contains(':'))
            {
                return false;
            }
            var (sessionKey, dbIndex) = ParseSessionKey(hashkey);
            var redisDb = _redisService.GetDatabase(dbIndex);

            var batch = redisDb.CreateBatch();

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
                await _redisService.HashDeleteAsync(redisDb, sessionKey);
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

            await _redisService.HashDeleteAsync(redisDb, sessionKey);
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
                Fingerprint = LinkFingerprintsAttributes([fingerprint4, fingerprint5])
            };

        }

        private async Task InsertRedis<T>(IDatabase redisDb, int dbIndex, string hashKey, string key, T value, int expiresInSecond, bool updateExpire)
        {
            if (updateExpire)
            {
                var expireAt = DateTimeOffset.Now.AddSeconds(expiresInSecond).DateTime;
                await _redisService.HashSetAndExpireAsync(_redisService.ShadowDatabase, $"{hashKey}:{dbIndex}", dbIndex, expiresInSecond);
                await _redisService.HashSetAsync(redisDb, hashKey, "ExpireAt", expireAt.ToString(CultureInfo.InvariantCulture), isForceUpdate: true);
            }

            await _redisService.HashSetAsync(redisDb, hashKey, key, value, isForceUpdate: true);
        }

        private (string, int) ParseSessionKey(string hashkey)
        {
            var parts = hashkey.Split(':');

            var sessionKey = string.Empty;
            var dbIndex = -1;

            if (parts.Length > 0)
            {
                sessionKey = parts[0];
                if (parts.Length > 1 && int.TryParse(parts[1], out var parsedIndex))
                {
                    dbIndex = parsedIndex;
                }
            }

            return (sessionKey, dbIndex);
        }
    }
}
