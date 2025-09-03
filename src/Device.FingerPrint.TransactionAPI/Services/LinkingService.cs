using Device.FingerPrint.TransactionAPI.Model;
using Device.FingerPrint.TransactionAPI.Queue;
using System.Globalization;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface ILinkingService
    {
        string FP4_REDIS_KEY { get; }
        string FP5_REDIS_KEY { get; }
        string TRANSACTION_REDIS_KEY { get; }

        Task Link(string sessionKey);
    }

    public class LinkingService : ILinkingService
    {
        public string FP4_REDIS_KEY { get; } = "FP4";
        public string FP5_REDIS_KEY { get; } = "FP5";
        public string TRANSACTION_REDIS_KEY { get; } = "Transaction";

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

        public async Task Link(string sessionKey)
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

                var fingerprint4 = await _redisService.HashGetAsync<APFingerprintModel>(sessionKey, FP4_REDIS_KEY);
                var fingerprint5 = await _redisService.HashGetAsync<APFingerprintModel>(sessionKey, FP5_REDIS_KEY);
                var transactionRequest = await _redisService.HashGetAsync<TransactionRequest>(sessionKey, TRANSACTION_REDIS_KEY);

                var deviceModel = new DeviceModel
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

                var fingerprintModel = new APFingerprintModel()
                {
                    Audio = fingerprint4.Audio,
                    Browser = fingerprint4.Browser,
                    Graphics = fingerprint5.Graphics,
                    Hardware = fingerprint4.Hardware,
                    Preferences = fingerprint4.Preferences
                };

                var messageQueue = new MessageQueue()
                {
                    Account = transactionRequest.Account,
                    Device = deviceModel,
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
                    FingerPrint = fingerprintModel
                };

                await _massTransitProducer.SendAsync(messageQueue, _appSettings.QueueName);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error linking session {sessionKey}", ex);
                throw;
            }
        }
    }
}
