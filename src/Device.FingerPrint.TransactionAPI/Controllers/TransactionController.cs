using Device.FingerPrint.TransactionAPI.Model;
using Device.FingerPrint.TransactionAPI.Queue;
using Device.FingerPrint.TransactionAPI.Services;
using Microsoft.AspNetCore.Mvc;
using ONEbook.UM.Models;
using ONEbook.UM.Services;

namespace Device.FingerPrint.TransactionAPI.Controllers
{
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly IMaintenanceService maintenanceService;
        private readonly ILogger logger;
        private readonly ILinkingService linkingService;

        public TransactionController(
            IMaintenanceService maintenanceService,
            IAppSettings appSettings,
            ILogger logger,
            ISubscriberService subscriberService,
            IMassTransitProducer massTransitProducer,
            IRedisService redisService,
            ILinkingService linkingService)
        {
            this.maintenanceService = maintenanceService;
            this.logger = logger;
            this.linkingService = linkingService;
        }

        [HttpPost("APFP/InsertTransaction")]
        public async Task<IActionResult> InsertTransactionAPFPAsync(TransactionRequest request)
        {
            try
            {
                if(string.IsNullOrEmpty(request.SessionKey))
                {
                    return BadRequest("SessionKey is required");
                }

                await linkingService.InsertTransaction(request.SessionKey, request);

                return Ok("Ok");
            }
            catch (Exception ex)
            {
                await logger.ErrorAsync("Error in InsertFP4", ex);
                return BadRequest();
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
