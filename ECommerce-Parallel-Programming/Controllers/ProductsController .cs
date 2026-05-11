


//using Ecommerce.Application.Use_Cases;
//using Microsoft.AspNetCore.Mvc;

//namespace ECommerce_Parallel_Programming.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class ProductsController : ControllerBase
//    {
//        private readonly GetProductsUseCase _useCase;
//        private readonly BuyProductUseCase _use;

//        public ProductsController(GetProductsUseCase useCase, BuyProductUseCase use)
//        {
//            _useCase = useCase;
//            _use = use;
//        }

//        // =========================
//        // GET PRODUCTS
//        // =========================
//        [HttpGet]
//        public async Task<IActionResult> GetProducts()
//        {
//            var products = await _useCase.GetAllProducts();
//            return Ok(products);
//        }

//        // =========================
//        // WITH THREAD (LOCK)
//        // =========================
//        [HttpPost("buy-with-thread")]
//        public async Task<IActionResult> BuyWithThread(
//            [FromQuery] int ProductId,
//            [FromQuery] int UserId,
//            [FromQuery] int quantity)
//        {
//            await _use.BuyProduct(UserId, ProductId, quantity);
//            return Ok("Success with protection");
//        }

//        // =========================
//        // WITHOUT THREAD
//        // =========================
//        [HttpPost("buy-without-thread")]
//        public async Task<IActionResult> BuyWithoutThread(
//            [FromQuery] int ProductId,
//            [FromQuery] int UserId,
//            [FromQuery] int quantity)
//        {
//            await _use.BuyProductWithoutThread(UserId, ProductId, quantity);
//            return Ok("Success without protection");
//        }

//        // =========================
//        //  (ASYNC QUEUE)
//        // =========================
//        [HttpPost("buy-blocking")]
//        public async Task<IActionResult> BuyBlocking(int ProductId, int UserId, int quantity)
//        {
//            var result = await _use.BuyProduct_Blocking(UserId, ProductId, quantity);
//            return Ok(result);
//        }
//        [HttpPost("buy-async")]
//        public async Task<IActionResult> BuyAsync(int ProductId, int UserId, int quantity)
//        {
//            var result = await _use.BuyProduct_WithAsyncQueue(UserId, ProductId, quantity);
//            return Ok(result);
//        }


//        [HttpGet("test-performance")]
//        public async Task<IActionResult> Test()
//        {
//            var sw = System.Diagnostics.Stopwatch.StartNew();

//            await _service.ProcessWithoutBatchAsync();

//            sw.Stop();
//            Console.WriteLine($"WITHOUT BATCH TIME: {sw.ElapsedMilliseconds} ms");

//            sw.Restart();

//            await _service.ProcessWithBatchAsync();

//            sw.Stop();
//            Console.WriteLine($"WITH BATCH TIME: {sw.ElapsedMilliseconds} ms");

//            return Ok("Check Console Output");
//        }



//    }
//}


using Ecommerce.Application.Use_Cases;
using Ecommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ECommerce_Parallel_Programming.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly GetProductsUseCase _useCase;
        private readonly BuyProductUseCase _use;
        private readonly SalesBatchProcessingService _service;

        public ProductsController(
            GetProductsUseCase useCase,
            BuyProductUseCase use,
            SalesBatchProcessingService service)
        {
            _useCase = useCase;
            _use = use;
            _service = service;
        }

        // =========================
        // GET PRODUCTS
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _useCase.GetAllProducts();
            return Ok(products);
        }

        // =========================
        // WITH THREAD (LOCK)
        // =========================
        [HttpPost("buy-with-thread")]
        public async Task<IActionResult> BuyWithThread(
            [FromQuery] int ProductId,
            [FromQuery] int UserId,
            [FromQuery] int quantity)
        {
            await _use.BuyProduct(UserId, ProductId, quantity);
            return Ok("Success with protection");
        }

        // =========================
        // WITHOUT THREAD
        // =========================
        [HttpPost("buy-without-thread")]
        public async Task<IActionResult> BuyWithoutThread(
            [FromQuery] int ProductId,
            [FromQuery] int UserId,
            [FromQuery] int quantity)
        {
            await _use.BuyProductWithoutThread(UserId, ProductId, quantity);
            return Ok("Success without protection");
        }

        // =========================
        // BLOCKING (ASYNC QUEUE)
        // =========================
        [HttpPost("buy-blocking")]
        public async Task<IActionResult> BuyBlocking(int ProductId, int UserId, int quantity)
        {
            var result = await _use.BuyProduct_Blocking(UserId, ProductId, quantity);
            return Ok(result);
        }

        // =========================
        // ASYNC QUEUE
        // =========================
        [HttpPost("buy-async")]
        public async Task<IActionResult> BuyAsync(int ProductId, int UserId, int quantity)
        {
            var result = await _use.BuyProduct_WithAsyncQueue(UserId, ProductId, quantity);
            return Ok(result);
        }

        // =========================
        // BATCH PERFORMANCE TEST
        // =========================
        [HttpGet("test-performance")]
        public async Task<IActionResult> Test()
        {
            var sw = Stopwatch.StartNew();

            await _service.ProcessWithoutBatchAsync();

            sw.Stop();
            Console.WriteLine($"WITHOUT BATCH TIME: {sw.ElapsedMilliseconds} ms");

            sw.Restart();

            await _service.ProcessWithBatchAsync(CancellationToken.None);

            sw.Stop();
            Console.WriteLine($"WITH BATCH TIME: {sw.ElapsedMilliseconds} ms");

            return Ok("Check Console Output");
        }
    }
}
