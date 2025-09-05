using Device.FingerPrint.TransactionAPI.Model;
using Device.FingerPrint.TransactionAPI.Queue;
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

        public MobileTransactionController(
            IAppSettings appSettings,
            ILogger logger,
            IMassTransitProducer massTransitProducer)
        {
            this.appSettings = appSettings;
            this.logger = logger;
            this.massTransitProducer = massTransitProducer;
        }

        [HttpPost("FPS/InsertMobileTransaction")]
        public IActionResult InsertTransactionAsync(MobileTransactionModel transaction)
        {
            if (transaction == null)
            {
                return BadRequest("Invalid transaction data.");
            }

            try
            {
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
