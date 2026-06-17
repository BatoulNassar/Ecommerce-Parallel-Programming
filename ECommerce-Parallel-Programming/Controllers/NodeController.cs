using Microsoft.AspNetCore.Mvc;

namespace ECommerce_Parallel_Programming.Controllers
{
    [ApiController]
    [Route("process")]
    public class NodeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Process()
        {
            var port = HttpContext.Connection.LocalPort;
            var message = $"Handled by node on port {port}";
            return Ok(new
            {
                Node = $"node-{port}",
                Port = port,
                Message = message,
                HandledAt = DateTime.Now
            });
        }
    }
}
