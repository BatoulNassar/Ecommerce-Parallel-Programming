using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Services
{
    public class TransactionDemoService
    {
        private readonly AppDbContext _context;

        public TransactionDemoService(AppDbContext context)
        {
            _context = context;
        }


        public async Task<string> BuyWithoutTransaction(int userId, int productId, int quantity, bool simulateFailure)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                throw new Exception("Product not found");
            if (product.Stock < quantity)
                throw new Exception("Not enough stock");

            // Step 1: Validate payment (simulated — always passes)
            var paymentAmount = product.Price * quantity;

            // Step 2: Update stock — this is SAVED IMMEDIATELY to the database
            product.Stock -= quantity;
            await _context.SaveChangesAsync();
            // At this point, stock is permanently reduced in the DB

            // Simulate a crash between stock update and order creation
            // (e.g., payment gateway timeout, network error, app crash)
            if (simulateFailure)
                throw new Exception(
                    "CRASH after stock update! Stock was reduced but no order was created. Data is inconsistent.");

            // Step 3: Create order
            var order = new Order
            {
                UserId = userId,
                ProductId = productId,
                Quantity = quantity,
                OrderDate = DateTime.Now,
                TotalPrice = paymentAmount,
                Status = "Completed"
            };
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            return "Order completed (no transaction protection)";
        }

 
        public async Task<string> BuyWithTransaction(int userId, int productId, int quantity, bool simulateFailure)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null)
                    throw new Exception("Product not found");
                if (product.Stock < quantity)
                    throw new Exception("Not enough stock");

                // Step 1: Validate payment (simulated)
                var paymentAmount = product.Price * quantity;

                // Step 2: Update stock
                // SaveChanges writes to DB but the TRANSACTION holds it
                // Other connections cannot see this change yet (Isolation)
                product.Stock -= quantity;
                await _context.SaveChangesAsync();

                // Simulate a crash — same failure as above
                if (simulateFailure)
                    throw new Exception(
                        "CRASH after stock update! But transaction will ROLLBACK — data stays consistent.");

                // Step 3: Create order
                var order = new Order
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity,
                    OrderDate = DateTime.Now,
                    TotalPrice = paymentAmount,
                    Status = "Completed"
                };
                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

                // ALL steps succeeded → make everything permanent
                await transaction.CommitAsync();

                return "Order completed (ACID transaction protected)";
            }
            catch
            {
                // Something failed → UNDO everything (stock goes back)
                await transaction.RollbackAsync();
                throw;
            }
        }

        // =====================================================
        // Helper methods for testing
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

        public async Task<int> GetOrderCount()
        {
            return await _context.Orders.CountAsync();
        }

        public async Task<int> DeleteTestOrders()
        {
            return await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM Orders WHERE UserId IN (1, 2)");
        }
    }
}
