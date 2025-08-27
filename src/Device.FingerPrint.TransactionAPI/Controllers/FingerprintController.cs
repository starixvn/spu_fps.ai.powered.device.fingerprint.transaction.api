using Microsoft.AspNetCore.Mvc;

namespace Device.FingerPrint.TransactionAPI.Controllers
{
    [ApiController]
    public class FingerprintController : ControllerBase
    {
        [HttpPost("APFP/InsertFP4")]
        public IActionResult InsertFP4(FingerprintRequest request)
        {
            return Ok();
        }

        [HttpPost("APFP/InsertFP5")]
        public IActionResult InsertFP5(FingerprintRequest request)
        {
            return Ok();
        }
    }

    public class FingerprintRequest
    {
        public required string SessionKey { get; set; }

        public string? GroupA { get; set; }

        public string? GroupB { get; set; }

        public string? GroupC { get; set; }

        public string? GroupD { get; set; }

        public string? GroupE { get; set; }
    }
}
