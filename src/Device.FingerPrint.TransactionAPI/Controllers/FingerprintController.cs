using Device.FingerPrint.TransactionAPI.Helpers;
using Device.FingerPrint.Model;
using Device.FingerPrint.TransactionAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Device.FingerPrint.TransactionAPI.Controllers
{
    [ApiController]
    public class FingerprintController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IFingerprintService _fingerprintService;
        public FingerprintController(ILogger logger, IFingerprintService fingerprintService)
        {
            _logger = logger;
            _fingerprintService = fingerprintService;
        }

        [HttpPost("APFP/InsertFP4")]
        public async Task<IActionResult> InsertFP4([FromForm] string request)
        {
            try
            {
                var decodedRequest = Base64.Decode(request);
                if (string.IsNullOrEmpty(decodedRequest))
                {
                    return BadRequest("Request is empty after decoding.");
                }

                var fingerprintRequest = JsonSerializer.Deserialize<FingerprintRequest>(decodedRequest);

                if (fingerprintRequest == null)
                {
                    return BadRequest("Invalid request format.");
                }

                var response = await _fingerprintService.InsertFP4(fingerprintRequest);

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("Error in InsertFP4", ex);
                return BadRequest(ex);
            }
        }

        [HttpPost("APFP/InsertFP5")]
        public async Task<IActionResult> InsertFP5([FromForm] string request)
        {
            try
            {
                var decodedRequest = Base64.Decode(request);
                if(string.IsNullOrEmpty(decodedRequest))
                {
                    return BadRequest("Request is empty after decoding.");
                }

                var fingerprintRequest = JsonSerializer.Deserialize<FingerprintRequest>(decodedRequest);

                if(fingerprintRequest == null)
                {
                    return BadRequest("Invalid request format.");
                }

                var response = await _fingerprintService.InsertFP5(fingerprintRequest);

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
