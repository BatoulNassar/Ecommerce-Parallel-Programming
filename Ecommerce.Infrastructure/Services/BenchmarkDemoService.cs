using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Ecommerce.Infrastructure.Services
{
    public class BenchmarkDemoService
    {
        private readonly AppDbContext _context;

        public BenchmarkDemoService(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // BEFORE OPTIMIZATION: Sequential Order Processing
        // =====================================================
        // Bottleneck: processes each order ONE BY ONE
        // Each order has simulated I/O work (10ms per order)
        // 100 orders = 100 * 10ms = ~1000ms total
        // =====================================================
        public async Task<BenchmarkResult> ProcessOrders_Sequential()
        {
            var trace = new List<TraceStep>();
            var totalSw = Stopwatch.StartNew();

            // Step 1: Fetch all orders from DB
            var stepSw = Stopwatch.StartNew();
            var orders = await _context.Orders.ToListAsync();
            stepSw.Stop();
            trace.Add(new TraceStep("DB: Fetch all orders", stepSw.ElapsedMilliseconds, $"{orders.Count} orders loaded"));

            // Step 2: Process each order SEQUENTIALLY (the bottleneck!)
            stepSw.Restart();
            decimal total = 0;
            foreach (var order in orders)
            {
                total += order.TotalPrice;
                await Task.Delay(10);
            }
            stepSw.Stop();
            trace.Add(new TraceStep("BOTTLENECK: Sequential processing (10ms x N orders)", stepSw.ElapsedMilliseconds, $"Processed {orders.Count} orders one-by-one"));

            // Step 3: Return result
            stepSw.Restart();
            var result = $"Total revenue: {total:C}";
            stepSw.Stop();
            trace.Add(new TraceStep("Calculate result", stepSw.ElapsedMilliseconds, result));

            totalSw.Stop();
            return new BenchmarkResult
            {
                Method = "BEFORE: Sequential Processing",
                TotalTimeMs = totalSw.ElapsedMilliseconds,
                OrderCount = orders.Count,
                Trace = trace
            };
        }

        // =====================================================
        // AFTER OPTIMIZATION: Batch + Parallel Processing
        // =====================================================
        // Fix: Group orders into batches, process batches in parallel
        // 100 orders / 50 per batch = 2 batches processed in parallel
        // Total time = ~10ms (one batch delay) instead of ~1000ms
        // =====================================================
        public async Task<BenchmarkResult> ProcessOrders_Optimized()
        {
            var trace = new List<TraceStep>();
            var totalSw = Stopwatch.StartNew();

            // Step 1: Fetch all orders from DB (same as before)
            var stepSw = Stopwatch.StartNew();
            var orders = await _context.Orders.ToListAsync();
            stepSw.Stop();
            trace.Add(new TraceStep("DB: Fetch all orders", stepSw.ElapsedMilliseconds, $"{orders.Count} orders loaded"));

            // Step 2: Split into batches and process IN PARALLEL
            stepSw.Restart();
            int batchSize = 50;
            var batches = orders
                .Select((o, i) => new { o, i })
                .GroupBy(x => x.i / batchSize)
                .Select(g => g.Select(x => x.o).ToList())
                .ToList();

            var batchResults = new decimal[batches.Count];

            var parallelTasks = batches.Select(async (batch, index) =>
            {
                batchResults[index] = batch.Sum(o => o.TotalPrice);
                await Task.Delay(10);
            });

            await Task.WhenAll(parallelTasks);
            decimal total = batchResults.Sum();
            stepSw.Stop();
            trace.Add(new TraceStep($"OPTIMIZED: Parallel batch processing ({batches.Count} batches)", stepSw.ElapsedMilliseconds, $"Processed {orders.Count} orders in {batches.Count} parallel batches of {batchSize}"));

            // Step 3: Return result
            stepSw.Restart();
            var result = $"Total revenue: {total:C}";
            stepSw.Stop();
            trace.Add(new TraceStep("Calculate result", stepSw.ElapsedMilliseconds, result));

            totalSw.Stop();
            return new BenchmarkResult
            {
                Method = "AFTER: Parallel Batch Processing",
                TotalTimeMs = totalSw.ElapsedMilliseconds,
                OrderCount = orders.Count,
                Trace = trace
            };
        }

        // =====================================================
        // BEFORE: Sequential single-product lookup
        // =====================================================
        // Fetches products one-by-one in a loop
        // =====================================================
        public async Task<BenchmarkResult> FetchProducts_Sequential()
        {
            var trace = new List<TraceStep>();
            var totalSw = Stopwatch.StartNew();

            var productIds = Enumerable.Range(0, 50).Select(i => (i % 3) + 1).ToArray();

            var stepSw = Stopwatch.StartNew();
            var products = new List<Product>();
            foreach (var id in productIds)
            {
                var p = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (p != null) products.Add(p);
                await Task.Delay(5);
            }
            stepSw.Stop();
            trace.Add(new TraceStep($"BOTTLENECK: {productIds.Length} sequential DB queries (5ms network latency each)", stepSw.ElapsedMilliseconds, $"Fetched {products.Count} products one-by-one"));

            totalSw.Stop();
            return new BenchmarkResult
            {
                Method = "BEFORE: Sequential Product Fetch (N queries)",
                TotalTimeMs = totalSw.ElapsedMilliseconds,
                OrderCount = productIds.Length,
                Trace = trace
            };
        }

        // =====================================================
        // AFTER: Single batch query
        // =====================================================
        // Fetches all products in ONE query using WHERE IN
        // =====================================================
        public async Task<BenchmarkResult> FetchProducts_Optimized()
        {
            var trace = new List<TraceStep>();
            var totalSw = Stopwatch.StartNew();

            var productIds = Enumerable.Range(0, 50).Select(i => (i % 3) + 1).ToArray();
            var uniqueIds = productIds.Distinct().ToList();

            var stepSw = Stopwatch.StartNew();
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => uniqueIds.Contains(p.Id))
                .ToListAsync();
            stepSw.Stop();
            trace.Add(new TraceStep("OPTIMIZED: Single batch query (WHERE IN)", stepSw.ElapsedMilliseconds, $"Fetched {products.Count} products in 1 query instead of {productIds.Length}"));

            totalSw.Stop();
            return new BenchmarkResult
            {
                Method = "AFTER: Batch Product Fetch (1 query)",
                TotalTimeMs = totalSw.ElapsedMilliseconds,
                OrderCount = productIds.Length,
                Trace = trace
            };
        }
    }

    public class TraceStep
    {
        public string Step { get; set; }
        public long TimeMs { get; set; }
        public string Detail { get; set; }

        public TraceStep(string step, long timeMs, string detail)
        {
            Step = step;
            TimeMs = timeMs;
            Detail = detail;
        }
    }

    public class BenchmarkResult
    {
        public string Method { get; set; }
        public long TotalTimeMs { get; set; }
        public int OrderCount { get; set; }
        public List<TraceStep> Trace { get; set; }
    }
}
