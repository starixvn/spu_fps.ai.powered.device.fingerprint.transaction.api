using Device.FingerPrint.TransactionAPI.Model;
using Device.FingerPrint.TransactionAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Device.FingerPrint.TransactionAPI.Controllers
{
    [ApiController]
    public class FingerprintController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IRedisService _redisService;
        private readonly ILinkingService _linkingService;
        public FingerprintController(ILogger logger, IRedisService redisService, ILinkingService linkingService)
        {
            _logger = logger;
            _redisService = redisService;
            _linkingService = linkingService;
        }

        [HttpPost("APFP/InsertFP4")]
        public async Task<IActionResult> InsertFP4(FingerprintRequest request)
        {
            try
            {
                var fingerprintJson = JsonSerializer.Serialize(request.FingerPrint);
                await _redisService.HashSetAsync<string>(request.SessionKey, _linkingService.FP4_REDIS_KEY, fingerprintJson, isForceUpdate: true);

                await _linkingService.Link(request.SessionKey);

                return Ok($"{request.SessionKey}");
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("Error in InsertFP4", ex);
                return BadRequest();
            }
        }

        [HttpPost("APFP/InsertFP5")]
        public async Task<IActionResult> InsertFP5(FingerprintRequest request)
        {
            try
            {
                var fingerprintJson = JsonSerializer.Serialize(request.FingerPrint);
                await _redisService.HashSetAsync<string>(request.SessionKey, _linkingService.FP5_REDIS_KEY, fingerprintJson, isForceUpdate: true);

                await _linkingService.Link(request.SessionKey);

                return Ok($"{request.SessionKey}");
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("Error in InsertFP5", ex);
                return BadRequest();
            }
        }
    }
}
