using Ecommerce.Application.Use_Cases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;
using System.Text.Json;

namespace ECommerce_Parallel_Programming.Controllers
{

    [ApiController]
    [Route("api/cached-products")]
    public class CachedProductsController : ControllerBase
    {
        private const string AllProductsKey = "products:all";
        private static readonly TimeSpan _ttl = TimeSpan.FromSeconds(60);

        private readonly GetProductsUseCase _useCase;
        private readonly IDistributedCache _cache;

        public CachedProductsController(GetProductsUseCase useCase, IDistributedCache cache)
        {
            _useCase = useCase;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var sw = Stopwatch.StartNew();

            var cached = await _cache.GetStringAsync(AllProductsKey);
            if (cached is not null)
            {
                sw.Stop();
                Console.WriteLine($"[CACHE HIT ] key={AllProductsKey} in {sw.ElapsedMilliseconds} ms");
                return Ok(new
                {
                    Source = "REDIS (cache hit)",
                    Key = AllProductsKey,
                    ElapsedMs = sw.ElapsedMilliseconds,
                    Data = JsonSerializer.Deserialize<object>(cached)
                });
            }

            Console.WriteLine($"[CACHE MISS] key={AllProductsKey} -> querying DB");
            var products = await _useCase.GetAllProducts();

            var json = JsonSerializer.Serialize(products);
            await _cache.SetStringAsync(AllProductsKey, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _ttl
            });

            sw.Stop();
            return Ok(new
            {
                Source = "DATABASE (cache miss, value now stored in Redis)",
                Key = AllProductsKey,
                TtlSeconds = (int)_ttl.TotalSeconds,
                ElapsedMs = sw.ElapsedMilliseconds,
                Data = products
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var key = $"products:{id}";
            var sw = Stopwatch.StartNew();

            var cached = await _cache.GetStringAsync(key);
            if (cached is not null)
            {
                sw.Stop();
                Console.WriteLine($"[CACHE HIT ] key={key} in {sw.ElapsedMilliseconds} ms");
                return Ok(new
                {
                    Source = "REDIS (cache hit)",
                    Key = key,
                    ElapsedMs = sw.ElapsedMilliseconds,
                    Data = JsonSerializer.Deserialize<object>(cached)
                });
            }

            Console.WriteLine($"[CACHE MISS] key={key} -> querying DB");
            var all = await _useCase.GetAllProducts();
            var product = all.FirstOrDefault(p => p.Id == id);
            if (product is null) return NotFound($"Product {id} not found.");

            var json = JsonSerializer.Serialize(product);
            await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _ttl
            });

            sw.Stop();
            return Ok(new
            {
                Source = "DATABASE (cache miss, value now stored in Redis)",
                Key = key,
                TtlSeconds = (int)_ttl.TotalSeconds,
                ElapsedMs = sw.ElapsedMilliseconds,
                Data = product
            });
        }

        [HttpDelete("cache")]
        public async Task<IActionResult> InvalidateAll()
        {
            await _cache.RemoveAsync(AllProductsKey);
            return Ok($"Removed cache key '{AllProductsKey}'.");
        }
    }
}
