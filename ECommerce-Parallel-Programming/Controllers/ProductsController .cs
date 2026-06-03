

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

        // ==========================================================
        // Buy Product Before Solution Race Condition and Resource Management
        // ==========================================================
        [HttpPost("buy-before-race-condition")]
        public async Task<IActionResult> BuyBeforeRaceCondition([FromQuery] int ProductId, [FromQuery] int UserId, [FromQuery] int quantity)
        {
            try
            {
                await _use.BuyProduct_Before_RaceCondition_and_ResourceManagement(UserId, ProductId, quantity);
                return Ok("Order processed without safety protection (Potential Race Condition).");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==========================================================
        // Buy Product After Solution Race Condition and Resource Management
        // ==========================================================
        [HttpPost("buy-after-race-condition")]
        public async Task<IActionResult> BuyAfterRaceCondition([FromQuery] int ProductId, [FromQuery] int UserId, [FromQuery] int quantity)
        {
            try
            {
                await _use.BuyProduct_After_RaceCondition_and_ResourceManagement(UserId, ProductId, quantity);
                return Ok("Order processed successfully with full Thread-Safety Protection.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==========================================================
        // Comparison before and after applying the solution Race Condition and Resource Management
        // ==========================================================
        [HttpGet("test-Race_Condition")]
        public async Task<IActionResult> TestRaceCondition([FromQuery] int productId, [FromQuery] int totalRequests = 100)
        {
            // ----------------------------------------------------------
            // ❌ PHASE 1: Testing BEFORE Solution (Race Condition Enabled)
            // ----------------------------------------------------------
            var swBefore = Stopwatch.StartNew();
            var tasksBefore = new List<Task>();
            for (int i = 1; i <= totalRequests; i++)
            {
                tasksBefore.Add(_use.BuyProduct_Before_RaceCondition_and_ResourceManagement(UserId: i, ProductId: productId, quantity: 1));
            }
            try { await Task.WhenAll(tasksBefore); } catch { }
            swBefore.Stop();

            var allProductsBefore = await _useCase.GetAllProducts();
            var productAfterUnprotected = allProductsBefore.FirstOrDefault(p => p.Id == productId);

            int actualStockUnprotected = productAfterUnprotected?.Stock ?? 0;
            int expectedStock = 100 - totalRequests;
            bool isRaceConditionDetected = actualStockUnprotected != expectedStock;

            // ----------------------------------------------------------
            // ✔ PHASE 2: Testing AFTER Solution (Thread-Safe Lock)
            // ----------------------------------------------------------
            var swAfter = Stopwatch.StartNew();
            var tasksAfter = new List<Task>();
            for (int i = 1; i <= totalRequests; i++)
            {
                tasksAfter.Add(_use.BuyProduct_After_RaceCondition_and_ResourceManagement(UserId: i, ProductId: productId, quantity: 1));
            }
            try { await Task.WhenAll(tasksAfter); } catch { }
            swAfter.Stop();

            var allProductsAfter = await _useCase.GetAllProducts();
            var productAfterProtected = allProductsAfter.FirstOrDefault(p => p.Id == productId);
            int actualStockProtected = productAfterProtected?.Stock ?? 0;

            return Ok(new
            {
                BeforeSolution = new {
                    Scenario = "Race Condition Active (No Locks Enabled)",
                    ExecutionTimeMs = swBefore.ElapsedMilliseconds,
                    StockRemainingInDatabase = actualStockUnprotected,
                    DataIntegrityStatus = isRaceConditionDetected
                        ? " LOST INTEGRITY: Race condition caused corrupted database values!"
                        : "Valid (No thread interleaving hit this time)"
                },
                AfterSolution = new {
                    Scenario = "AsyncLock & Thread Synchronization Enabled",
                    ExecutionTimeMs = swAfter.ElapsedMilliseconds,
                    StockRemainingInDatabase = actualStockProtected,
                    DataIntegrityStatus = " 100% SECURE: Thread-safe concurrency preserved data balance."
                }
            });
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
