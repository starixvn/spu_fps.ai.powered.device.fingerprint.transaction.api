using Device.FingerPrint.TransactionAPI.Model;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace Device.FingerPrint.TransactionAPI.Controllers
{
    [ApiController]
    public class CommonController(IAppSettings appSettings) : ControllerBase
    {
        private readonly IAppSettings appSettings = appSettings;

        [HttpGet("whoami")]
        public WhoamiModel Index()
        {
            var httpConnectionFeature = HttpContext.Features.Get<IHttpConnectionFeature>();
            var localIpAddress = httpConnectionFeature?.LocalIpAddress;

            return new WhoamiModel
            {
                Environment = appSettings.Environment,
                ServerName = Environment.MachineName,
                ServerTime = DateTime.Now,
                LocalIP = localIpAddress?.ToString()
            };
        }
    }
}
