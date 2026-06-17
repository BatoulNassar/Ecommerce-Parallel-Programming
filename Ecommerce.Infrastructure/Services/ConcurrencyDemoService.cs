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

        // =====================================================
        // OPTIMISTIC LOCK (RowVersion)
        // =====================================================
        // How it works:
        //   1. Read the product (including its RowVersion)
        //   2. Update the stock
        //   3. SaveChanges → EF sends: UPDATE ... WHERE Id=@id AND RowVersion=@oldVersion
        //   4. If another instance changed the row first, RowVersion won't match
        //      → EF throws DbUpdateConcurrencyException
        //   5. We catch it and RETRY with fresh data
        //
        // This is like saying: "I'll try to update, but if someone beat me to it,
        // I'll reload and try again."
        // =====================================================
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

        // =====================================================
        // PESSIMISTIC LOCK — DISTRIBUTED (sp_getapplock)
        // =====================================================
        // How it works:
        //   1. Start a database TRANSACTION
        //   2. Call sp_getapplock → SQL Server gives us a NAMED lock
        //      Example: lock name = "Product_1" (for product with Id=1)
        //   3. Since ALL instances share the same database,
        //      only ONE instance can hold "Product_1" lock at a time
        //   4. Other instances WAIT until the lock is released
        //   5. Lock is automatically released when transaction commits/rollbacks
        //
        // This is like saying: "Database, hold everyone else back until I'm done
        // with this product."
        // =====================================================
        public async Task<string> BuyWithPessimisticLock(int userId, int productId, int quantity)
        {
            // Step 1: Start a database transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Step 2: Acquire a distributed lock from SQL Server
                // sp_getapplock creates a named lock within the database
                // @Resource  = the lock name (we use "Product_{id}" so each product has its own lock)
                // @LockMode  = 'Exclusive' means only one holder at a time
                // @LockTimeout = 10000ms (10 seconds) max wait time
                // @LockOwner = 'Transaction' means lock lives as long as the transaction
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

        // =====================================================
        // RESET stock (helper for testing)
        // Uses raw SQL to bypass RowVersion check, and clears
        // the change tracker so next queries get fresh data.
        // =====================================================
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
