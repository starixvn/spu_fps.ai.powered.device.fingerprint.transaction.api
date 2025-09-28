using Device.FingerPrint.Model;
using StackExchange.Redis;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface IFingerprintService
    {
        Task<string> InsertFP4(FingerprintRequest fingerprint);

        Task<string> InsertFP5(FingerprintRequest fingerprint);
    }
    public class FingerprintService : IFingerprintService
    {
        private readonly int _fingerprintTtlInSeconds;

        private readonly IRedisMultiplexDbService _redisService;
        private readonly ILinkingService _linkingService;

        public FingerprintService(
            IAppSettings appSettings,
            IRedisMultiplexDbService redisService,
            ILinkingService linkingService)
        {
            _redisService = redisService;
            _linkingService = linkingService;

            _fingerprintTtlInSeconds = appSettings.RedisSettings.FingerprintTTLInSeconds;
        }

        public async Task<string> InsertFP4(FingerprintRequest fingerprint)
        {
            var redisDB = _redisService.FP4Database;
            await InsertRedis(redisDB, fingerprint.Hashed, fingerprint.Fingerprint);

            await _linkingService.LinkingOnInsert(fingerprint.SessionKey);

            return fingerprint.Hashed;
        }

        public async Task<string> InsertFP5(FingerprintRequest fingerprint)
        {
            var redisDB = _redisService.FP5Database;
            await InsertRedis(redisDB, fingerprint.Hashed, fingerprint.Fingerprint);

            await _linkingService.LinkingOnInsert(fingerprint.SessionKey);

            return fingerprint.Hashed;
        }

        private async Task InsertRedis(IDatabase database, string hashKey, APFingerprintModel fingerprint)
        {
            await _redisService.SetAndExpireAsync(database, hashKey, fingerprint, _fingerprintTtlInSeconds);
        }
    }
}
