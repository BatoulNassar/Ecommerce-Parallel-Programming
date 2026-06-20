using ECommerce_Parallel_Programming.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce_Parallel_Programming.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PerformanceController : ControllerBase
    {
        [HttpGet("summary")]
        public IActionResult GetSummary()
        {
            return Ok(PerformanceStore.GetSummary());
        }

        [HttpGet("logs")]
        public IActionResult GetLogs()
        {
            return Ok(PerformanceStore.GetLogs().TakeLast(50));
        }

        [HttpDelete("clear")]
        public IActionResult Clear()
        {
            PerformanceStore.Clear();
            return Ok("Performance logs cleared");
        }
    }
}
