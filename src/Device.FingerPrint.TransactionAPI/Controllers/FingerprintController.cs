using Microsoft.AspNetCore.Mvc;

namespace Device.FingerPrint.TransactionAPI.Controllers
{
    [ApiController]
    public class FingerprintController : ControllerBase
    {
        [HttpPost("APFP/InsertFP4")]
        public IActionResult InsertFP4(FingerprintRequest request)
        {
            return Ok("Ok");
        }

        [HttpPost("APFP/InsertFP5")]
        public IActionResult InsertFP5(FingerprintRequest request)
        {
            return Ok("Ok");
        }
    }

    public class Fingperprint
    {

        public string? Audio { get; set; }

        public string? Browser { get; set; }

        public string? Hardware { get; set; }

        public string? Preferences { get; set; }

        public string? Graphics { get; set; }
    }

    public class FingerprintRequest
    {
        public required string SessionKey { get; set; }

        public required string Hashed { get; set; }

        public required Fingperprint FingerPrint { get; set; }

    }
}
