using Device.FingerPrint.Model;
using Device.FingerPrint.TransactionAPI.Helpers;
using Device.FingerPrint.TransactionAPI.Model;
using Device.FingerPrint.TransactionAPI.Queue;
using Device.FingerPrint.TransactionAPI.Services;
using Fanex.Logging.Extensions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace Device.FingerPrint.TransactionAPI.Controllers
{
    [ApiController]
    public class MobileTransactionController : ControllerBase
    {
        private readonly IAppSettings appSettings;
        private readonly ILogger logger;
        private readonly IMassTransitProducer massTransitProducer;
        private readonly ISubscriberService subscriberService;

        public MobileTransactionController(
            IAppSettings appSettings,
            ILogger logger,
            IMassTransitProducer massTransitProducer,
            ISubscriberService subscriberService)
        {
            this.appSettings = appSettings;
            this.logger = logger;
            this.massTransitProducer = massTransitProducer;
            this.subscriberService = subscriberService;
        }

        [HttpPost("FPS/InsertMobileTransaction")]
        public async Task<IActionResult> InsertTransactionAsync(MobileTransactionRequest request)
        {
            if (request == null)
            {
                return BadRequest();
            }

            try
            {
                var transactionJson = Base64.NormalDecode(request.Transaction);
                var transaction = JsonConvert.DeserializeObject<MobileTransactionModel>(transactionJson);
                if (transaction == null)
                {
                    var message = new LogMessage("InsertMobileTransaction transaction is null")
                        .AddMessage($"request {JsonConvert.SerializeObject(request)}");
                    await logger.ErrorAsync(message.ToString());

                    return BadRequest();
                }

                if (appSettings.EnableMBDebugLogging && !string.IsNullOrEmpty(transaction.Exceptions))
                {
                    var message = new LogMessage("Mobile App SDK Occurs Exception")
                        .AddMessage($"request {transactionJson}");

                    await logger.InfoAsync(message.ToString());
                }

                var subscriberId = await subscriberService.ValidateAsync(transaction.Subscriber);
                if (subscriberId <= 0)
                {
                    var message = new LogMessage("InsertMobileTransaction Subscriber invalid")
                        .AddMessage($"transaction {JsonConvert.SerializeObject(transaction)}");
                    await logger.ErrorAsync(message.ToString());

                    return BadRequest();
                }

                transaction.TransDate = DateTimeOffset.Now.DateTime;
                transaction.IP = HttpContext.Request.Headers["Fps-Forwarded-For"].ToString();

                PushToQueueAsync(transaction);

                return Ok();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);

                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("FPS/MobileTransaction/Base64")]
        public string MobileTransactionToBase64(MobileTransactionModel transaction)
        {
            if (transaction == null)
            {
                return "Invalid transaction data.";
            }

            return ObjectToBase64(transaction);
        }

        private void PushToQueueAsync(MobileTransactionModel transactionRequest)
        {
            var message = new MessageQueue
            {
                Account = transactionRequest.Account,
                Action = transactionRequest.Action,
                ActionResult = transactionRequest.ActionResult,
                Subscriber = transactionRequest.Subscriber,
                IP = transactionRequest.IP,
                TransDate = transactionRequest.TransDate,
                SubscriberDate = DateTimeOffset.Now,
                Mobile = new MobileModel
                {
                    MachineCode = !string.IsNullOrEmpty(transactionRequest.AndroidCode) ? transactionRequest.AndroidCode : transactionRequest.MachineCode,
                    Brand = transactionRequest.Brand,
                    Country = transactionRequest.Country,
                    CustomCode = transactionRequest.CustomCode,
                    IsEmulator = transactionRequest.IsEmulator,
                    IsMobileAppTrans = true,
                    Manufacturer = transactionRequest.Manufacturer,
                    MediaDRMCode = transactionRequest.MediaDRMCode,
                    ModelName = transactionRequest.ModelName,
                    OS = transactionRequest.OS,
                    SdkVersion = transactionRequest.SdkVersion,
                    SystemLanguage = transactionRequest.SystemLanguage,
                    Timezone = transactionRequest.Timezone,
                    DeviceDetails = transactionRequest.DeviceDetails
                }
            };

            massTransitProducer.Send(message, appSettings.MobileQueueName);
        }


        private static string ObjectToBase64(MobileTransactionModel transaction)
        {
            string jsonString = JsonConvert.SerializeObject(transaction);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
            string base64EncodedString = Convert.ToBase64String(jsonBytes);

            return base64EncodedString;
        }
    }
}
