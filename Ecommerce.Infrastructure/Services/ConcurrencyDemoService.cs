using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Services
{
    public class ConcurrencyDemoService
    {
        private readonly AppDbContext _context;

        public ConcurrencyDemoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> BuyWithOptimisticLock(int userId, int productId, int quantity)
        {
            int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Detach any previously tracked Product entity
                    // (important on retry — we need fresh data from DB)
                    var tracked = _context.ChangeTracker
                        .Entries<Product>()
                        .FirstOrDefault(e => e.Entity.Id == productId);

                    if (tracked != null)
                        tracked.State = EntityState.Detached;

                    // Read fresh product from database
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.Id == productId);

                    if (product == null)
                        throw new Exception("Product not found");

                    if (product.Stock < quantity)
                        throw new Exception("Not enough stock");

                    // Update stock
                    product.Stock -= quantity;

                    // SaveChanges will CHECK the RowVersion automatically
                    // If someone else updated this product, this line throws!
                    await _context.SaveChangesAsync();

                    // If we reach here, stock update succeeded — create the order
                    var order = new Order
                    {
                        UserId = userId,
                        ProductId = productId,
                        Quantity = quantity,
                        OrderDate = DateTime.Now,
                        TotalPrice = product.Price * quantity,
                        Status = "Completed"
                    };
                    await _context.Orders.AddAsync(order);
                    await _context.SaveChangesAsync();

                    return $"Optimistic Lock: Order completed (attempt {attempt})";
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Another instance updated the product at the same time!
                    // We retry with fresh data
                    if (attempt == maxRetries)
                        throw new Exception(
                            $"Optimistic Lock: Failed after {maxRetries} retries — too many concurrent updates");
                }
            }

            throw new Exception("Optimistic Lock: Unexpected failure");
        }

        public async Task<string> BuyWithPessimisticLock(int userId, int productId, int quantity)
        {
            // Step 1: Start a database transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {

                var lockName = $"Product_{productId}";

                await _context.Database.ExecuteSqlRawAsync(
                    @"DECLARE @res INT;
                      EXEC @res = sp_getapplock
                          @Resource = {0},
                          @LockMode = 'Exclusive',
                          @LockTimeout = 10000,
                          @LockOwner = 'Transaction';
                      IF @res < 0
                          THROW 50000, 'Could not acquire distributed lock', 1;",
                    lockName);

                // Step 3: Now we have the lock — safe to read and update
                // No other instance can reach this point for the same product
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null)
                    throw new Exception("Product not found");

                if (product.Stock < quantity)
                    throw new Exception("Not enough stock");

                // Step 4: Update stock and create order (within the same transaction)
                product.Stock -= quantity;

                var order = new Order
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity,
                    OrderDate = DateTime.Now,
                    TotalPrice = product.Price * quantity,
                    Status = "Completed"
                };
                await _context.Orders.AddAsync(order);

                await _context.SaveChangesAsync();

                // Step 5: Commit → this also RELEASES the lock
                await transaction.CommitAsync();

                return "Distributed Lock: Order completed successfully";
            }
            catch
            {
                // Rollback → also releases the lock
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ResetProductStock(int productId, int stock)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE Products SET Stock = {0} WHERE Id = {1}",
                stock, productId);
            _context.ChangeTracker.Clear();
        }

        public async Task<int> GetProductStock(int productId)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);
            return product?.Stock ?? 0;
        }
    }
}
