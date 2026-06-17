using Microsoft.AspNetCore.Mvc;

namespace ECommerce_Parallel_Programming.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class LoadDistributorController : ControllerBase
    {
        private static int _counter = -1;

        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _config;

        public LoadDistributorController(IHttpClientFactory httpFactory, IConfiguration config)
        {
            _httpFactory = httpFactory;
            _config = config;
        }

        [HttpGet("distribute")]
        public async Task<IActionResult> Distribute([FromQuery] int totalRequests = 6)
        {
            var nodes = _config.GetSection("LoadDistributor:Nodes").Get<string[]>()
                        ?? new[] { "http://localhost:8000", "http://localhost:8001", "http://localhost:8002" };

            if (nodes.Length == 0)
                return BadRequest("No nodes configured under LoadDistributor:Nodes.");

            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var results = new List<object>();

            for (int i = 1; i <= totalRequests; i++)
            {
                // Round Robin: atomically advance and pick the next node.
                int index = Interlocked.Increment(ref _counter) % nodes.Length;
                if (index < 0) index += nodes.Length;

                var node = nodes[index].TrimEnd('/');
                var url = $"{node}/process";

                string body;
                bool ok;
                try
                {
                    var resp = await client.GetAsync(url);
                    body = await resp.Content.ReadAsStringAsync();
                    ok = resp.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    body = $"ERROR contacting {url}: {ex.Message}";
                    ok = false;
                }

                results.Add(new
                {
                    Task = i,
                    SentTo = url,
                    Success = ok,
                    Response = body
                });
            }

            return Ok(new
            {
                Strategy = "Round Robin",
                Nodes = nodes,
                TotalRequests = totalRequests,
                Results = results
            });
        }
    }
}
