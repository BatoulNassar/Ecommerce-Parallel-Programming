using System.Collections.Concurrent;
using System.Diagnostics;

namespace ECommerce_Parallel_Programming.Middleware
{
    // =====================================================
    // AOP: Performance Monitoring Middleware
    // =====================================================
    // This is Aspect-Oriented Programming (AOP) because:
    //
    //   1. CROSS-CUTTING CONCERN: Performance monitoring applies
    //      to ALL endpoints, not just one specific feature
    //
    //   2. SEPARATION: The monitoring logic is completely separate
    //      from business logic — controllers don't know about it
    //
    //   3. DECLARATIVE: One line in Program.cs enables it for
    //      every request: app.UseMiddleware<PerformanceMonitoringMiddleware>()
    //
    //   4. NON-INVASIVE: No controller code was changed. We simply
    //      "wrap" every request with timing logic before and after.
    //
    // How it works:
    //   Request comes in → Middleware starts a Stopwatch
    //   → Request is processed by the controller
    //   → Middleware stops the Stopwatch
    //   → Logs the timing and adds it to response headers
    //   → Stores the log for the /api/performance endpoint
    // =====================================================
    public class PerformanceMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMonitoringMiddleware> _logger;

        public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            var method = context.Request.Method;
            var path = context.Request.Path;

            await _next(context);

            sw.Stop();
            var statusCode = context.Response.StatusCode;
            var elapsed = sw.ElapsedMilliseconds;

            var log = new RequestLog
            {
                Timestamp = DateTime.Now,
                Method = method,
                Path = path,
                StatusCode = statusCode,
                DurationMs = elapsed,
                IsSlow = elapsed > 500
            };

            PerformanceStore.AddLog(log);

            if (elapsed > 500)
                _logger.LogWarning("SLOW REQUEST: {Method} {Path} took {Duration}ms (status {Status})",
                    method, path, elapsed, statusCode);
        }
    }

    // =====================================================
    // In-memory store for performance logs
    // Thread-safe using ConcurrentQueue
    // =====================================================
    public static class PerformanceStore
    {
        private static readonly ConcurrentQueue<RequestLog> _logs = new();
        private const int MaxLogs = 200;

        public static void AddLog(RequestLog log)
        {
            _logs.Enqueue(log);
            while (_logs.Count > MaxLogs)
                _logs.TryDequeue(out _);
        }

        public static List<RequestLog> GetLogs() => _logs.ToList();

        public static object GetSummary()
        {
            var logs = _logs.ToList();
            if (logs.Count == 0)
                return new { Message = "No requests recorded yet" };

            var byEndpoint = logs
                .GroupBy(l => $"{l.Method} {l.Path}")
                .Select(g => new
                {
                    Endpoint = g.Key,
                    TotalRequests = g.Count(),
                    AvgResponseMs = Math.Round(g.Average(l => l.DurationMs), 1),
                    MinMs = g.Min(l => l.DurationMs),
                    MaxMs = g.Max(l => l.DurationMs),
                    SlowRequests = g.Count(l => l.IsSlow),
                    ErrorCount = g.Count(l => l.StatusCode >= 400)
                })
                .OrderByDescending(x => x.AvgResponseMs)
                .ToList();

            return new
            {
                TotalRequests = logs.Count,
                AverageResponseMs = Math.Round(logs.Average(l => l.DurationMs), 1),
                SlowRequests = logs.Count(l => l.IsSlow),
                FastestMs = logs.Min(l => l.DurationMs),
                SlowestMs = logs.Max(l => l.DurationMs),
                ByEndpoint = byEndpoint
            };
        }

        public static void Clear() => _logs.Clear();
    }

    public class RequestLog
    {
        public DateTime Timestamp { get; set; }
        public string Method { get; set; }
        public string Path { get; set; }
        public int StatusCode { get; set; }
        public long DurationMs { get; set; }
        public bool IsSlow { get; set; }
    }
}
