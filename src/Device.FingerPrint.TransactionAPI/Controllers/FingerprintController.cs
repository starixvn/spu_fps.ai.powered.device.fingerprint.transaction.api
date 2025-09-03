using Device.FingerPrint.TransactionAPI.Model;
using Device.FingerPrint.TransactionAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace Device.FingerPrint.TransactionAPI.Controllers
{
    [ApiController]
    public class FingerprintController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly ILinkingService _linkingService;
        public FingerprintController(ILogger logger, ILinkingService linkingService)
        {
            _logger = logger;
            _linkingService = linkingService;
        }

        [HttpPost("APFP/InsertFP4")]
        public async Task<IActionResult> InsertFP4(FingerprintRequest request)
        {
            try
            {
                await _linkingService.InsertFingerprint(request.SessionKey, _linkingService.FP4_REDIS_KEY, request.Fingerprint);

                await _linkingService.LinkingOnInsert(request.SessionKey);

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
                await _linkingService.InsertFingerprint(request.SessionKey, _linkingService.FP5_REDIS_KEY, request.Fingerprint);

                await _linkingService.LinkingOnInsert(request.SessionKey);

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
