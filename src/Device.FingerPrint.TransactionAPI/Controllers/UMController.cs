using Microsoft.AspNetCore.Mvc;
using ONEbook.UM.Models;
using ONEbook.UM.Services;

namespace Device.FingerPrint.TransactionAPI.Controllers
{
    [ApiController]
    public class UMController : ControllerBase
    {
        private readonly IMaintenanceService maintenanceService;

        public UMController(
            IMaintenanceService maintenanceService)
        {
            this.maintenanceService = maintenanceService;
        }


        [HttpGet]
        [Route("FPS/TestUM")]
        public UnderMaintenanceTime TestUM()
        {
            return maintenanceService.GetUnderMaintenanceTime();
        }
    }
}
