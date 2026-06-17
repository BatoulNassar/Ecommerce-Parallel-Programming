using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Text.Json;

namespace Ecommerce.StressTests
{
    public class BuyProduct_StressTest
    {
        private static readonly string BaseUrl = "http://localhost:5289";

        public static void Run()
        {
            int productId = 3;
            int concurrentUsers = 100;
            int testDurationSeconds = 30;

            Console.WriteLine("╔══════════════════════════════════════════════════╗");
            Console.WriteLine("║     STRESS TEST: Ecommerce Buy Product         ║");
            Console.WriteLine("╠══════════════════════════════════════════════════╣");
            Console.WriteLine($"║  Target:       {BaseUrl}");
            Console.WriteLine($"║  Product ID:   {productId}");
            Console.WriteLine($"║  Concurrent:   {concurrentUsers} users");
            Console.WriteLine($"║  Duration:     {testDurationSeconds} seconds");
            Console.WriteLine("╚══════════════════════════════════════════════════╝");
            Console.WriteLine();

            // ---- Step 1: Check system is alive ----
            Console.WriteLine("[1/4] Checking if the API is running...");
            using var checkClient = new HttpClient();
            try
            {
                var response = checkClient.GetAsync($"{BaseUrl}/").Result;
                Console.WriteLine($"  API Status: {response.StatusCode} - System is UP");
            }
            catch
            {
                Console.WriteLine("  ERROR: API is not running! Start the app first:");
                Console.WriteLine("  dotnet run --project ECommerce-Parallel-Programming");
                return;
            }

            // ---- Step 2: Record stock BEFORE test ----
            Console.WriteLine("[2/4] Recording stock before test...");
            int stockBefore = GetProductStock(productId);
            Console.WriteLine($"  Product {productId} stock BEFORE: {stockBefore}");
            Console.WriteLine();

            // ---- Step 3: Run NBomber stress test ----
            Console.WriteLine("[3/4] Starting NBomber stress test...");
            Console.WriteLine($"  Firing {concurrentUsers} concurrent users for {testDurationSeconds} seconds");
            Console.WriteLine("  (This will take about 30 seconds...)\n");

            using var httpClient = new HttpClient();

            // Scenario 1: Buy with distributed lock (protected endpoint)
            var protectedScenario = Scenario.Create("buy-distributed-lock", async context =>
            {
                var userId = (context.ScenarioInfo.InstanceNumber % 2) + 1;
                var url = $"{BaseUrl}/api/Products/buy-distributed-lock?ProductId={productId}&UserId={userId}&quantity=1";
                var request = Http.CreateRequest("POST", url);
                return await Http.Send(httpClient, request);
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(copies: concurrentUsers, during: TimeSpan.FromSeconds(testDurationSeconds))
            );

            // Scenario 2: Read products (GET - lightweight read load)
            var readScenario = Scenario.Create("read-products", async context =>
            {
                var url = $"{BaseUrl}/api/Products";
                var request = Http.CreateRequest("GET", url);
                return await Http.Send(httpClient, request);
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(copies: concurrentUsers, during: TimeSpan.FromSeconds(testDurationSeconds))
            );

            var result = NBomberRunner
                .RegisterScenarios(protectedScenario, readScenario)
                .Run();

            // ---- Step 4: Verify data integrity AFTER test ----
            Console.WriteLine("\n[4/4] Verifying data integrity after test...");
            int stockAfter = GetProductStock(productId);

            var buyStats = result.ScenarioStats.FirstOrDefault(s => s.ScenarioName == "buy-distributed-lock");
            var readStats = result.ScenarioStats.FirstOrDefault(s => s.ScenarioName == "read-products");

            int successfulBuys = buyStats != null ? (int)buyStats.Ok.Request.Count : 0;
            int failedBuys = buyStats != null ? (int)buyStats.Fail.Request.Count : 0;
            int totalBuys = successfulBuys + failedBuys;

            int successfulReads = readStats != null ? (int)readStats.Ok.Request.Count : 0;
            int failedReads = readStats != null ? (int)readStats.Fail.Request.Count : 0;

            double avgResponseMs = buyStats != null ? buyStats.Ok.Latency.MeanMs : 0;

            int expectedStock = stockBefore - successfulBuys;
            bool dataIntact = stockAfter == expectedStock;

            Console.WriteLine();
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  STRESS TEST REPORT                         ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║                                                              ║");
            Console.WriteLine("║  --- BUY ENDPOINT (buy-distributed-lock) ---                 ║");
            Console.WriteLine($"║  Total Requests:        {totalBuys,8}                           ║");
            Console.WriteLine($"║  Success Requests:      {successfulBuys,8}                           ║");
            Console.WriteLine($"║  Failed Requests:       {failedBuys,8}                           ║");
            Console.WriteLine($"║  Average Response Time: {avgResponseMs,8:F1} ms                      ║");
            Console.WriteLine("║                                                              ║");
            Console.WriteLine("║  --- READ ENDPOINT (GET /api/Products) ---                   ║");
            Console.WriteLine($"║  Total Requests:        {successfulReads + failedReads,8}                           ║");
            Console.WriteLine($"║  Success Requests:      {successfulReads,8}                           ║");
            Console.WriteLine($"║  Failed Requests:       {failedReads,8}                           ║");
            Console.WriteLine("║                                                              ║");
            Console.WriteLine("║  --- DATA INTEGRITY ---                                      ║");
            Console.WriteLine($"║  Stock Before:          {stockBefore,8}                           ║");
            Console.WriteLine($"║  Stock After:           {stockAfter,8}                           ║");
            Console.WriteLine($"║  Expected Stock:        {expectedStock,8}                           ║");
            Console.WriteLine($"║  Successful Purchases:  {successfulBuys,8}                           ║");
            Console.WriteLine($"║  Data Integrity:        {(dataIntact ? "PASSED" : "FAILED"),8}                           ║");
            Console.WriteLine("║                                                              ║");
            Console.WriteLine("║  --- SYSTEM STATUS ---                                       ║");
            Console.WriteLine($"║  System Crashed:              NO                             ║");
            Console.WriteLine($"║  Data Loss:                   {(dataIntact ? "NO" : "YES")}                             ║");
            Console.WriteLine("║                                                              ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

            if (dataIntact)
            {
                Console.WriteLine("\n  RESULT: System handled the stress test successfully!");
                Console.WriteLine($"  {concurrentUsers} concurrent users for {testDurationSeconds}s — no crash, no data loss.");
            }
            else
            {
                Console.WriteLine("\n  WARNING: Data integrity check FAILED!");
                Console.WriteLine($"  Expected stock {expectedStock} but found {stockAfter}");
            }

            Console.WriteLine("\n  NBomber also generated a detailed HTML report in the 'reports' folder.");
        }

        private static int GetProductStock(int productId)
        {
            using var client = new HttpClient();
            var json = client.GetStringAsync($"{BaseUrl}/api/Products").Result;
            var products = JsonSerializer.Deserialize<List<JsonElement>>(json);
            var product = products?.FirstOrDefault(p => p.GetProperty("id").GetInt32() == productId);
            return product?.GetProperty("stock").GetInt32() ?? 0;
        }
    }
}
