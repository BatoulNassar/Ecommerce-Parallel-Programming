

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
        private readonly ConcurrencyDemoService _concurrency;
        private readonly TransactionDemoService _transaction;
        private readonly IServiceScopeFactory _scopeFactory;

        public ProductsController(
            GetProductsUseCase useCase,
            BuyProductUseCase use,
            SalesBatchProcessingService service,
            ConcurrencyDemoService concurrency,
            TransactionDemoService transaction,
            IServiceScopeFactory scopeFactory)
        {
            _useCase = useCase;
            _use = use;
            _service = service;
            _concurrency = concurrency;
            _transaction = transaction;
            _scopeFactory = scopeFactory;
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

        // ==========================================================
        // TASK 7: Concurrency Control — Optimistic Lock (RowVersion)
        // ==========================================================
        [HttpPost("buy-optimistic-lock")]
        public async Task<IActionResult> BuyOptimisticLock(
            [FromQuery] int ProductId, [FromQuery] int UserId, [FromQuery] int quantity)
        {
            try
            {
                var result = await _concurrency.BuyWithOptimisticLock(UserId, ProductId, quantity);
                var port = HttpContext.Connection.LocalPort;
                return Ok($"[Port {port}] {result}");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==========================================================
        // TASK 7: Concurrency Control — Pessimistic Distributed Lock
        // ==========================================================
        [HttpPost("buy-distributed-lock")]
        public async Task<IActionResult> BuyDistributedLock(
            [FromQuery] int ProductId, [FromQuery] int UserId, [FromQuery] int quantity)
        {
            try
            {
                var result = await _concurrency.BuyWithPessimisticLock(UserId, ProductId, quantity);
                var port = HttpContext.Connection.LocalPort;
                return Ok($"[Port {port}] {result}");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==========================================================
        // TASK 7: Test — Compare No Lock vs Optimistic vs Distributed
        // Each concurrent task gets its own DI scope (= own DbContext),
        // simulating real concurrent HTTP requests from different users.
        // ==========================================================
        [HttpGet("test-concurrency-control")]
        public async Task<IActionResult> TestConcurrencyControl(
            [FromQuery] int productId = 1,
            [FromQuery] int totalRequests = 50)
        {
            int initialStock = 200;
            int successCount, failCount;

            // ---- TEST 1: No Lock (Race Condition) ----
            await _concurrency.ResetProductStock(productId, initialStock);

            successCount = 0; failCount = 0;
            var swNoLock = Stopwatch.StartNew();

            var tasksNoLock = Enumerable.Range(1, totalRequests).Select(async i =>
            {
                using var scope = _scopeFactory.CreateScope();
                var useCase = scope.ServiceProvider.GetRequiredService<BuyProductUseCase>();
                try
                {
                    await useCase.BuyProduct_Before_RaceCondition_and_ResourceManagement(
                        UserId: (i % 2) + 1, ProductId: productId, quantity: 1);
                    Interlocked.Increment(ref successCount);
                }
                catch { Interlocked.Increment(ref failCount); }
            });
            await Task.WhenAll(tasksNoLock);
            swNoLock.Stop();

            var stockAfterNoLock = await _concurrency.GetProductStock(productId);
            int expectedStock = initialStock - successCount;

            var noLockResult = new
            {
                Scenario = "No Lock (Race Condition)",
                TimeTakenMs = swNoLock.ElapsedMilliseconds,
                SuccessOrders = successCount,
                FailedOrders = failCount,
                ExpectedStock = expectedStock,
                ActualStock = stockAfterNoLock,
                DataCorrupted = stockAfterNoLock != expectedStock
            };

            // ---- TEST 2: Optimistic Lock (RowVersion) ----
            await _concurrency.ResetProductStock(productId, initialStock);

            successCount = 0; failCount = 0;
            var swOptimistic = Stopwatch.StartNew();

            var tasksOptimistic = Enumerable.Range(1, totalRequests).Select(async i =>
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<ConcurrencyDemoService>();
                try
                {
                    await svc.BuyWithOptimisticLock(
                        userId: (i % 2) + 1, productId: productId, quantity: 1);
                    Interlocked.Increment(ref successCount);
                }
                catch { Interlocked.Increment(ref failCount); }
            });
            await Task.WhenAll(tasksOptimistic);
            swOptimistic.Stop();

            var stockAfterOptimistic = await _concurrency.GetProductStock(productId);
            expectedStock = initialStock - successCount;

            var optimisticResult = new
            {
                Scenario = "Optimistic Lock (RowVersion)",
                TimeTakenMs = swOptimistic.ElapsedMilliseconds,
                SuccessOrders = successCount,
                FailedOrders = failCount,
                ExpectedStock = expectedStock,
                ActualStock = stockAfterOptimistic,
                DataCorrupted = stockAfterOptimistic != expectedStock
            };

            // ---- TEST 3: Pessimistic Distributed Lock (sp_getapplock) ----
            await _concurrency.ResetProductStock(productId, initialStock);

            successCount = 0; failCount = 0;
            var swPessimistic = Stopwatch.StartNew();

            var tasksPessimistic = Enumerable.Range(1, totalRequests).Select(async i =>
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<ConcurrencyDemoService>();
                try
                {
                    await svc.BuyWithPessimisticLock(
                        userId: (i % 2) + 1, productId: productId, quantity: 1);
                    Interlocked.Increment(ref successCount);
                }
                catch { Interlocked.Increment(ref failCount); }
            });
            await Task.WhenAll(tasksPessimistic);
            swPessimistic.Stop();

            var stockAfterPessimistic = await _concurrency.GetProductStock(productId);
            expectedStock = initialStock - successCount;

            var pessimisticResult = new
            {
                Scenario = "Pessimistic Distributed Lock (sp_getapplock)",
                TimeTakenMs = swPessimistic.ElapsedMilliseconds,
                SuccessOrders = successCount,
                FailedOrders = failCount,
                ExpectedStock = expectedStock,
                ActualStock = stockAfterPessimistic,
                DataCorrupted = stockAfterPessimistic != expectedStock
            };

            var port = HttpContext.Connection.LocalPort;
            return Ok(new
            {
                InstancePort = port,
                TotalConcurrentRequests = totalRequests,
                Results = new[] { noLockResult, optimisticResult, pessimisticResult }
            });
        }

        // ==========================================================
        // TASK 8: Transaction Integrity — Buy WITHOUT transaction
        // ==========================================================
        [HttpPost("buy-without-transaction")]
        public async Task<IActionResult> BuyWithoutTransaction(
            [FromQuery] int ProductId, [FromQuery] int UserId,
            [FromQuery] int quantity, [FromQuery] bool simulateFailure = false)
        {
            try
            {
                var result = await _transaction.BuyWithoutTransaction(UserId, ProductId, quantity, simulateFailure);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==========================================================
        // TASK 8: Transaction Integrity — Buy WITH transaction (ACID)
        // ==========================================================
        [HttpPost("buy-with-transaction")]
        public async Task<IActionResult> BuyWithTransaction(
            [FromQuery] int ProductId, [FromQuery] int UserId,
            [FromQuery] int quantity, [FromQuery] bool simulateFailure = false)
        {
            try
            {
                var result = await _transaction.BuyWithTransaction(UserId, ProductId, quantity, simulateFailure);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==========================================================
        // TASK 8: Test — Compare WITHOUT vs WITH transaction
        // Simulates a failure mid-operation and checks data integrity
        // ==========================================================
        [HttpGet("test-transaction-integrity")]
        public async Task<IActionResult> TestTransactionIntegrity(
            [FromQuery] int productId = 1)
        {
            int initialStock = 200;

            // ---- PHASE 1: WITHOUT Transaction (failure causes broken data) ----
            await _transaction.ResetProductStock(productId, initialStock);
            var ordersBefore1 = await _transaction.GetOrderCount();

            string error1 = "";
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<TransactionDemoService>();
                await svc.BuyWithoutTransaction(
                    userId: 1, productId: productId, quantity: 5, simulateFailure: true);
            }
            catch (Exception ex) { error1 = ex.Message; }

            var stockAfterNoTx = await _transaction.GetProductStock(productId);
            var ordersAfterNoTx = await _transaction.GetOrderCount();

            var noTransactionResult = new
            {
                Scenario = "WITHOUT Transaction (simulateFailure = true)",
                InitialStock = initialStock,
                StockAfterFailure = stockAfterNoTx,
                StockReduced = stockAfterNoTx < initialStock,
                OrdersCreated = ordersAfterNoTx - ordersBefore1,
                DataConsistent = false,
                Problem = "Stock was reduced by 5 but NO order was created — data is BROKEN",
                Error = error1
            };

            // ---- PHASE 2: WITH Transaction (failure causes clean rollback) ----
            await _transaction.ResetProductStock(productId, initialStock);
            var ordersBefore2 = await _transaction.GetOrderCount();

            string error2 = "";
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<TransactionDemoService>();
                await svc.BuyWithTransaction(
                    userId: 1, productId: productId, quantity: 5, simulateFailure: true);
            }
            catch (Exception ex) { error2 = ex.Message; }

            var stockAfterTx = await _transaction.GetProductStock(productId);
            var ordersAfterTx = await _transaction.GetOrderCount();

            var withTransactionResult = new
            {
                Scenario = "WITH Transaction / ACID (simulateFailure = true)",
                InitialStock = initialStock,
                StockAfterFailure = stockAfterTx,
                StockReduced = stockAfterTx < initialStock,
                OrdersCreated = ordersAfterTx - ordersBefore2,
                DataConsistent = stockAfterTx == initialStock,
                Result = "Transaction ROLLED BACK — stock unchanged, no orphan order. Data is SAFE",
                Error = error2
            };

            // ---- PHASE 3: WITH Transaction (no failure — happy path) ----
            await _transaction.ResetProductStock(productId, initialStock);
            var ordersBefore3 = await _transaction.GetOrderCount();

            string result3 = "";
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<TransactionDemoService>();
                result3 = await svc.BuyWithTransaction(
                    userId: 1, productId: productId, quantity: 5, simulateFailure: false);
            }
            catch (Exception ex) { result3 = ex.Message; }

            var stockAfterSuccess = await _transaction.GetProductStock(productId);
            var ordersAfterSuccess = await _transaction.GetOrderCount();

            var successResult = new
            {
                Scenario = "WITH Transaction / ACID (simulateFailure = false — happy path)",
                InitialStock = initialStock,
                StockAfterSuccess = stockAfterSuccess,
                ExpectedStock = initialStock - 5,
                OrdersCreated = ordersAfterSuccess - ordersBefore3,
                DataConsistent = stockAfterSuccess == initialStock - 5 && (ordersAfterSuccess - ordersBefore3) == 1,
                Result = result3
            };

            // ---- PHASE 4: Concurrent ACID test ----
            await _transaction.ResetProductStock(productId, initialStock);
            var ordersBefore4 = await _transaction.GetOrderCount();
            int successCount = 0, failCount = 0;

            var concurrentTasks = Enumerable.Range(1, 20).Select(async i =>
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<TransactionDemoService>();
                try
                {
                    await svc.BuyWithTransaction(
                        userId: (i % 2) + 1, productId: productId, quantity: 1, simulateFailure: false);
                    Interlocked.Increment(ref successCount);
                }
                catch { Interlocked.Increment(ref failCount); }
            });
            await Task.WhenAll(concurrentTasks);

            var stockAfterConcurrent = await _transaction.GetProductStock(productId);
            var ordersAfterConcurrent = await _transaction.GetOrderCount();
            var newOrders = ordersAfterConcurrent - ordersBefore4;

            var concurrentResult = new
            {
                Scenario = "20 Concurrent Transactions (ACID under load)",
                InitialStock = initialStock,
                StockAfterTest = stockAfterConcurrent,
                ExpectedStock = initialStock - successCount,
                SuccessfulOrders = successCount,
                FailedOrders = failCount,
                NewOrdersInDB = newOrders,
                DataConsistent = stockAfterConcurrent == (initialStock - successCount) && newOrders == successCount
            };

            return Ok(new
            {
                Task = "Task 8: Transaction Integrity / ACID",
                Phase1_WithoutTransaction = noTransactionResult,
                Phase2_WithTransaction_Failure = withTransactionResult,
                Phase3_WithTransaction_Success = successResult,
                Phase4_ConcurrentACID = concurrentResult
            });
        }
    }
}
