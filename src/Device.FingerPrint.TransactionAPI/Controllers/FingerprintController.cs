using Device.FingerPrint.TransactionAPI.Model;
using Device.FingerPrint.TransactionAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Device.FingerPrint.TransactionAPI.Controllers
{
    [ApiController]
    public class FingerprintController : ControllerBase
    {
        private readonly ILogger logger;
        private readonly IRedisService redisService;
        private readonly ILinkingService linkingService;

        public FingerprintController(ILogger logger, IRedisService redisService, ILinkingService linkingService)
        {
            this.logger = logger;
            this.redisService = redisService;
            this.linkingService = linkingService;
        }

        [HttpPost("APFP/InsertFP4")]
        public async Task<IActionResult> InsertFP4(FingerprintRequest request)
        {
            try
            {
                var fingerprintJson = JsonSerializer.Serialize(request.FingerPrint);
                await redisService.HashSetAsync<string>(request.SessionKey, "FP4", fingerprintJson, isForceUpdate: true);
                return Ok("Ok");
            }
            catch (Exception ex)
            {
                await logger.ErrorAsync("Error in InsertFP4", ex);
                return BadRequest();
            }
        }

        [HttpPost("APFP/InsertFP5")]
        public async Task<IActionResult> InsertFP5(FingerprintRequest request)
        {
            try
            {
                var fingerprintJson = JsonSerializer.Serialize(request.FingerPrint);
                await redisService.HashSetAsync<string>(request.SessionKey, "FP5", fingerprintJson, isForceUpdate: true);
                return Ok("Ok");
            }
            catch (Exception ex)
            {
                await logger.ErrorAsync("Error in InsertFP4", ex);
                return BadRequest();
            }
        }
    }
}
