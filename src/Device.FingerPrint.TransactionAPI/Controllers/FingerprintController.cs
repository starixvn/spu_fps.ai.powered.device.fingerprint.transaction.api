using Device.FingerPrint.TransactionAPI.Helpers;
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
        private readonly ILinkingService _linkingService;
        public FingerprintController(ILogger logger, ILinkingService linkingService)
        {
            _logger = logger;
            _linkingService = linkingService;
        }

        [HttpPost("APFP/InsertFP4")]
        public async Task<IActionResult> InsertFP4([FromBody]string request)
        {
            try
            {
                var decodedRequest = Base64.Decode(request);
                var fingerprintRequest = JsonSerializer.Deserialize<FingerprintRequest>(decodedRequest);

                var response = await _linkingService.InsertFingerprint(fingerprintRequest.SessionKey, _linkingService.FP4_REDIS_KEY, fingerprintRequest.Fingerprint);

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("Error in InsertFP4", ex);
                return BadRequest(ex);
            }
        }

        [HttpPost("APFP/InsertFP5")]
        public async Task<IActionResult> InsertFP5([FromBody]string request)
        {
            try
            {
                var fingerprintRequest = JsonSerializer.Deserialize<FingerprintRequest>(Base64.Decode(request));

                var response = await _linkingService.InsertFingerprint(fingerprintRequest.SessionKey, _linkingService.FP5_REDIS_KEY, fingerprintRequest.Fingerprint);

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("Error in InsertFP5", ex);
                return BadRequest(ex);
            }
        }
    }
}
