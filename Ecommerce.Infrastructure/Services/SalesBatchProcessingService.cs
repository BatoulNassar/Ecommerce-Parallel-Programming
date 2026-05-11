using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Infrastructure.Services
{
    public class SalesBatchProcessingService
    {
        private readonly AppDbContext _context;

        public SalesBatchProcessingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task ProcessWithoutBatchAsync()
        {
            Console.WriteLine("WITHOUT BATCH STARTED");

            var orders = await _context.Orders.ToListAsync();

            decimal total = 0;

            foreach (var order in orders)
            {
                total += order.TotalPrice;
                await Task.Delay(10);
            }

            Console.WriteLine($"WITHOUT BATCH TOTAL: {total}");
            Console.WriteLine("WITHOUT BATCH DONE");
        }

        public async Task ProcessWithBatchAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("BATCH STARTED");

            var orders = await _context.Orders.ToListAsync(cancellationToken);

            int batchSize = 50;

            var batches = orders
                .Select((o, i) => new { o, i })
                .GroupBy(x => x.i / batchSize)
                .Select(g => g.Select(x => x.o).ToList());

            decimal total = 0;

            foreach (var batch in batches)
            {
                Console.WriteLine($"Processing batch size: {batch.Count}");

                total += batch.Sum(x => x.TotalPrice);

                await Task.Delay(10);
            }

            Console.WriteLine($"BATCH TOTAL: {total}");
            Console.WriteLine("BATCH DONE");
        }

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            await ProcessWithBatchAsync(cancellationToken);
        }
    }
}