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
        private readonly ILinkingService linkingService;
        private readonly IDeviceService deviceService;
        private readonly IMassTransitProducer massTransitProducer;
        private readonly IAppSettings appSettings;
        private readonly IRedisMultiplexDbService redisMultiplexDbService;

        public TransactionController(
            IMaintenanceService maintenanceService,
            ILogger logger,
            ILinkingService linkingService,
            IDeviceService deviceService,
            IMassTransitProducer massTransitProducer,
            IAppSettings appSettings,
            IRedisMultiplexDbService redisMultiplexDbService
            )
        {
            this.maintenanceService = maintenanceService;
            this.logger = logger;
            this.linkingService = linkingService;
            this.deviceService = deviceService;
            this.massTransitProducer = massTransitProducer;
            this.appSettings = appSettings;
            this.redisMultiplexDbService = redisMultiplexDbService;
        }

        [HttpPost("APFP/InsertTransaction")]
        public async Task<IActionResult> InsertTransactionAPFPAsync(TransactionRequest request)
        {
            try
            {
                if (!appSettings.Environment.Equals("production", StringComparison.CurrentCultureIgnoreCase))
                {
                    await redisMultiplexDbService.DefaultDatabase.HashIncrementAsync($"TransactionAPI::{request.Account}::{request.TransDate:dd-MM-yyyy}", "TotalTransaction", 1);
                }
                await redisMultiplexDbService.DefaultDatabase.HashIncrementAsync("TransactionAPI", "InsertTransaction", 1);
            }
            catch (Exception ex)
            {
                await logger.ErrorAsync("Error in Counter Insert Transaction APFP", ex);
            }

            try
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
                if(deviceInfo is not null && !string.IsNullOrEmpty(deviceInfo.Version))
                {
                    var majorVersion = deviceInfo.Version.Split('.').FirstOrDefault();
                    if(int.TryParse(majorVersion, out int ver) && ver < 3)
                    {
                        isNewFlow = false;
                    }
                }

                if (!isNewFlow)
                {
                    var deviceModel = TransformDeviceModel(request);
                    await PushToQueueAsync(request, deviceModel, deviceInfo);

                    return Ok();
                }

                if (string.IsNullOrEmpty(request.SessionKey))
                {
                    return BadRequest("SessionKey is required");
                }

                var response = await linkingService.InsertTransaction(request.SessionKey, request);

                return Ok(response);
            }
            catch (Exception ex)
            {
                await logger.ErrorAsync("Error in Insert Transaction", ex);
                return BadRequest();
            }
        }
        
        [HttpGet]
        [Route("FPS/TestUM")]
        public UnderMaintenanceTime TestUM()
        {
            return maintenanceService.GetUnderMaintenanceTime();
        }

        private static DeviceModel TransformDeviceModel(TransactionRequest transRequest)
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

        private async Task PushToQueueAsync(TransactionRequest transactionRequest, DeviceModel deviceModel, DeviceInfoModel deviceInfo)
        {
            var dataEvent = new MessageQueue
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
                DeviceInfo = deviceInfo,
                IsNewFlow = transactionRequest.IsNewFlow ?? false,
            };

            await massTransitProducer.SendAsync(dataEvent, appSettings.QueueName);
        }
    }
}
