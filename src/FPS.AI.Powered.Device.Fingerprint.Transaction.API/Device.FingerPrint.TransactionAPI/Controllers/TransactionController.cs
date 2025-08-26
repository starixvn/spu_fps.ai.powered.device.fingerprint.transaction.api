using Device.FingerPrint.TransactionAPI.Model;
using Device.FingerPrint.TransactionAPI.Queue;
using Device.FingerPrint.TransactionAPI.Services;
using Microsoft.AspNetCore.Mvc;
using ONEbook.UM.Models;
using ONEbook.UM.Services;
using System.Globalization;
using System.Text.Json;

namespace Device.FingerPrint.TransactionAPI.Controllers
{
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly IMaintenanceService maintenanceService;
        private readonly IAppSettings appSettings;
        private readonly Fanex.Logging.ILogger logger;
        private readonly ISubscriberService subscriberService;
        private readonly IMassTransitProducer massTransitProducer;

        public TransactionController(
            IMaintenanceService maintenanceService,
            IAppSettings appSettings,
            Fanex.Logging.ILogger logger,
            ISubscriberService subscriberService,
            IMassTransitProducer massTransitProducer)
        {
            this.maintenanceService = maintenanceService;
            this.appSettings = appSettings;
            this.logger = logger;
            this.subscriberService = subscriberService;
            this.massTransitProducer = massTransitProducer;
        }

        [HttpPost("FPS/AddTransaction")]
        [HttpPost("FPS/AddTransactionVer2")]
        [HttpPost("FPS/CheckTransaction")]
        public async Task<IActionResult> CheckTransactionAsync(TransactionRequest transRequest)
        {
            var responseCode = TransactionResult.UnderMaintenance;

            if (!IsMaintenance && !IsIgnoredSite(transRequest.Subscriber))
            {
                try
                {
                    responseCode = await subscriberService.ValidateAsync(transRequest);

                    if (responseCode == TransactionResult.Success)
                    {
                        var deviceModel = TransformDeviceModel(transRequest);

                        await PushToQueueAsync(transRequest, deviceModel);
                    }
                    else
                    {
                        var errorMessage = GetTransactionResponse(responseCode);
                        await logger.ErrorAsync($"CheckTransactionAsync responseCode = {JsonSerializer.Serialize(errorMessage)} \nTransRequest: {JsonSerializer.Serialize(transRequest)}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message, ex);

                    responseCode = TransactionResult.Others;
                }
            }

            var response = GetTransactionResponse(responseCode);
            return Ok(response.ResultMessage);
        }

        [HttpPost("FPS/InsertTransaction")]
        public IActionResult InsertTransactionAsync(TransactionRequest transRequest)
        {
            var deviceModel = TransformDeviceModel(transRequest);

            _ = PushToQueueAsync(transRequest, deviceModel);

            return Ok();
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

        private bool IsIgnoredSite(string site)
        {
            var ignoredSites = appSettings.IgnoredSites.Split(',');

            if (ignoredSites[0].Equals("*", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return Array.Exists(ignoredSites, s => string.Equals(s, site, StringComparison.OrdinalIgnoreCase));
        }   

        private async Task PushToQueueAsync(TransactionRequest transactionRequest, DeviceModel deviceModel)
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
            };

            await massTransitProducer.SendAsync(dataEvent, appSettings.QueueName);
        }

        private TransactionResponse GetTransactionResponse(string code)
        {
            return new TransactionResponse
            {
                ResultCode = code,
                ResultMessage = TransactionResult.GetMessage(code)
            };
        }

        private bool IsMaintenance
        {
            get
            {
                var downTime = maintenanceService.GetUnderMaintenanceTime();

                return downTime.IsUnderMaintenanceTime || appSettings.IsUM;
            }
        }


        [HttpGet]
        [Route("FPS/TestUM")]
        public UnderMaintenanceTime TestUM()
        {
            return maintenanceService.GetUnderMaintenanceTime();
        }
    }
}
