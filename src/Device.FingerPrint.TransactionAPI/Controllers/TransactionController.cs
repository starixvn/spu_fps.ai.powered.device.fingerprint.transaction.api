using Device.FingerPrint.Model;
using Device.FingerPrint.TransactionAPI.Queue;
using Device.FingerPrint.TransactionAPI.Services;
using Microsoft.AspNetCore.Mvc;
using ONEbook.UM.Models;
using ONEbook.UM.Services;
using System.Globalization;

namespace Device.FingerPrint.TransactionAPI.Controllers
{
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly IMaintenanceService maintenanceService;
        private readonly ILogger logger;
        private readonly ITransactionService transactionService;
        private readonly IDeviceService deviceService;
        private readonly IMassTransitProducer massTransitProducer;
        private readonly IAppSettings appSettings;
        private readonly IRedisMultiplexDbService redisMultiplexDbService;
        private readonly IExpiredTransactionService expiredTransactionService;

#pragma warning disable S107 // Methods should not have too many parameters
        public TransactionController(
            IMaintenanceService maintenanceService,
            ILogger logger,
            ITransactionService transactionService,
            IDeviceService deviceService,
            IMassTransitProducer massTransitProducer,
            IAppSettings appSettings,
            IRedisMultiplexDbService redisMultiplexDbService,
            IExpiredTransactionService expiredTransactionService
            )
#pragma warning restore S107 // Methods should not have too many parameters
        {
            this.maintenanceService = maintenanceService;
            this.logger = logger;
            this.transactionService = transactionService;
            this.deviceService = deviceService;
            this.massTransitProducer = massTransitProducer;
            this.appSettings = appSettings;
            this.redisMultiplexDbService = redisMultiplexDbService;
            this.expiredTransactionService = expiredTransactionService;
        }

        [HttpPost("APFP/InsertTransaction")]
        public async Task<IActionResult> InsertTransactionAPFPAsync(TransactionRequest request)
        {
            await CountTransaction(request);

            try
            {
                var transaction = TransformTransactionModel(request);

                if (!transaction.IsNewFlow)
                {
                    await PushToQueueAsync(transaction);

                    return Ok();
                }

                if (string.IsNullOrEmpty(request.SessionKey))
                {
                    return BadRequest("SessionKey is required");
                }

                await transactionService.InsertTransaction(transaction);

                return Ok();
            }
            catch (Exception ex)
            {
                await logger.ErrorAsync("Error in Insert Transaction", ex);
                return BadRequest();
            }
        }

        [HttpPost("APFP/ScanExpiredTransaction")]
        public async Task<IActionResult> ScanExpiredTransactionAsync(int pageSize = 100)
        {
            try
            {
                await expiredTransactionService.ScanExpiredTransaction(pageSize);
                return Ok();
            }
            catch (Exception ex)
            {
                await logger.ErrorAsync("Error in Scan Expired Transaction", ex);
                return BadRequest();
            }
        }

        [HttpGet]
        [Route("FPS/TestUM")]
        public UnderMaintenanceTime TestUM()
        {
            return maintenanceService.GetUnderMaintenanceTime();
        }

        private TransactionModel TransformTransactionModel(TransactionRequest request)
        {
            var di = request.Di ?? request.__di;

            var deviceInfo = deviceService.GetDeviceInfo(di);
            if (string.IsNullOrEmpty(deviceInfo.UserAgent))
            {
                deviceInfo.UserAgent = request.UserAgent;
            }

            if (!string.IsNullOrEmpty(request.Bi))
            {
                deviceInfo.BotD = deviceService.TransformBotdModel(request.Bi);
            }

            var isNewFlow = true;
            if (deviceInfo is not null && !string.IsNullOrEmpty(deviceInfo.Version))
            {
                var majorVersion = deviceInfo.Version.Split('.').FirstOrDefault();
                if (int.TryParse(majorVersion, out int ver) && ver < 3)
                {
                    isNewFlow = false;
                }
            }

            var deviceModel = TransformDeviceModel(request);

            return new TransactionModel
            {
                Account = request.Account,
                Action = request.Action,
                ActionResult = request.ActionResult,
                AcceptsTypes = request.AcceptsTypes,
                Platform = request.Platform,
                UserAgent = request.UserAgent,
                Screen = request.Screen,
                Ip = request.Ip,
                FakeIp = request.FakeIp,
                Url = request.Url,
                Di = di,
                Bi = request.Bi,
                Token = request.Token,
                DetecasAnalysis = request.DetecasAnalysis,
                Subscriber = request.Subscriber,
                SubscriberId = request.SubscriberId,
                LoginTime = request.LoginTime,
                CaptchaResult = request.CaptchaResult,
                SessionKey = request.SessionKey,
                TransDate = request.TransDate,
                IsNewFlow = isNewFlow,
                DeviceInfo = deviceInfo,
                Device = deviceModel
            };
        }

        private DeviceModel TransformDeviceModel(TransactionRequest transRequest)
        {
            return new DeviceModel
            {
                AcceptsTypes = transRequest.AcceptsTypes,
                DetecasAnalysis = transRequest.DetecasAnalysis,
                Di = transRequest.Di ?? transRequest.__di,
                Ip = transRequest.Ip,
                Url = transRequest.Url,
                Platform = transRequest.Platform,
                Screen = transRequest.Screen,
                TotalMinutes = (DateTimeOffset.UtcNow - DateTimeOffset.Now).TotalMinutes.ToString(CultureInfo.InvariantCulture),
                UserAgent = transRequest.UserAgent
            };
        }

        private async Task PushToQueueAsync(TransactionModel transaction)
        {
            var dataEvent = new MessageQueue
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
                DeviceInfo = transaction.DeviceInfo,
                IsNewFlow = transaction.IsNewFlow,
            };

            await massTransitProducer.SendAsync(dataEvent, appSettings.QueueName);
        }

        private async Task CountTransaction(TransactionRequest request)
        {
            try
            {
                if (!appSettings.Environment.Equals("production", StringComparison.CurrentCultureIgnoreCase))
                {
                    await redisMultiplexDbService.DefaultDatabase.HashIncrementAsync($"TransactionAPI::{request.Account}::{request.TransDate}", "TotalTransaction", 1);
                    await redisMultiplexDbService.DefaultDatabase.KeyExpireAsync($"TransactionAPI::{request.Account}::{request.TransDate}", TimeSpan.FromDays(1));
                }
                await redisMultiplexDbService.DefaultDatabase.HashIncrementAsync("TransactionAPI", "InsertTransaction", 1);
            }
            catch (Exception ex)
            {
                await logger.ErrorAsync("Error in Counter Insert Transaction APFP", ex);
            }
        }
    }
}
