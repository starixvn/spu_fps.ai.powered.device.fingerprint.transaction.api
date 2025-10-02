using Device.FingerPrint.Model;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace Device.FingerPrint.TransactionAPI.Controllers
{
    [ApiController]
    public class CommonController(IAppSettings appSettings) : ControllerBase
    {
        private readonly IAppSettings appSettings = appSettings;
        private static readonly string CachedServerName = Environment.MachineName;

        [HttpGet("whoami")]
        public WhoamiModel Index()
        {
            string? localIpString = null;
            try
            {
                var httpConnectionFeature = HttpContext?.Features?.Get<IHttpConnectionFeature>();
                var localIpAddress = httpConnectionFeature?.LocalIpAddress;
                localIpString = localIpAddress?.ToString();
            }
            catch
            {
                // If getting the HTTP connection feature or local IP address fails, leave it as null
                localIpString = null;
            }

            return new WhoamiModel
            {
                Environment = appSettings.Environment,
                ServerName = CachedServerName,
                ServerTime = DateTimeOffset.Now,
                LocalIP = localIpString
            };
        }
    }
}
