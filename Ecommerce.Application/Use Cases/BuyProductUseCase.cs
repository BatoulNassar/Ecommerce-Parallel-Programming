

using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Application.Use_Cases
{
    public class BuyProductUseCase
    {
        private readonly IProductRepository _repo;
        private readonly IOrderRepository _orderRepo;
        private readonly IBackgroundTaskQueue _queue;

        private static readonly int _coreCount = Environment.ProcessorCount;
        private static readonly int _maxConcurrency = _coreCount * (1 + 4);

        private static readonly SemaphoreSlim _capacityThrottle =
            new SemaphoreSlim(_maxConcurrency, _maxConcurrency);

        private static readonly SemaphoreSlim _asyncLock =
            new SemaphoreSlim(1, 1);

        public BuyProductUseCase(
            IProductRepository repo,
            IOrderRepository orderRepo,
            IBackgroundTaskQueue queue)
        {
            _repo = repo;
            _orderRepo = orderRepo;
            _queue = queue;
        }

        // ================================
        // ❌ PROBLEM VERSION (BEFORE)
        // ================================
        public async Task BuyProduct(int UserId, int ProductId, int quantity)
        {
            await _capacityThrottle.WaitAsync();
            try
            {
                await _asyncLock.WaitAsync();
                try
                {
                    var product = await _repo.GetProductById(ProductId);

                    if (product.Stock >= quantity)
                    {
                        product.Stock -= quantity;
                        await _repo.UpdateStockForProduct(product);

                        var Order = new Order
                        {
                            UserId = UserId,
                            ProductId = ProductId,
                            Quantity = quantity,
                            OrderDate = DateTime.Now,
                            TotalPrice = product.Price * quantity,
                        };

                        await _orderRepo.AddOrder(Order);

                     
                        Console.WriteLine("Generating Invoice...");
                        await File.WriteAllTextAsync(
                            $"invoice_{Order.Id}.txt",
                            $"Invoice For User {UserId} - Total = {Order.TotalPrice}");

                        Console.WriteLine("Invoice Generated");
                    }
                    else
                    {
                        throw new Exception("The requested quantity is not available in stock.");
                    }
                }
                finally
                {
                    _asyncLock.Release();
                }
            }
            finally
            {
                _capacityThrottle.Release();
            }
        }

        // ================================
        // ❌ TEST VERSION (optional)
        // ================================
        public async Task BuyProductWithoutThread(int UserId, int ProductId, int quantity)
        {
            var product = await _repo.GetProductById(ProductId);

            if (product.Stock >= quantity)
            {
                product.Stock -= quantity;

                await _repo.UpdateStockForProduct(product);

                var Order = new Order
                {
                    UserId = UserId,
                    ProductId = ProductId,
                    Quantity = quantity,
                    OrderDate = DateTime.Now,
                    TotalPrice = product.Price * quantity,
                };

                await _orderRepo.AddOrder(Order);
            }
            else
            {
                throw new Exception("The requested quantity is not available in stock.");
            }
        }

      

        public async Task<string> BuyProduct_WithAsyncQueue(int UserId, int ProductId, int quantity)
        {
            var product = await _repo.GetProductById(ProductId);

            if (product.Stock < quantity)
                return "Failed: Not enough stock";

           
            product.Stock -= quantity;

            await _repo.UpdateStockForProduct(product);

           
            var order = new Order
            {
                UserId = UserId,
                ProductId = ProductId,
                Quantity = quantity,
                OrderDate = DateTime.Now,
                TotalPrice = product.Price * quantity,
                Status = "Pending"
            };

            await _orderRepo.AddOrder(order);

          
            Console.WriteLine("Order Accepted - Processing in background...");

          
            _queue.QueueBackgroundWorkItem(async (sp, token) =>
            {
                Console.WriteLine("BACKGROUND START");

                Console.WriteLine("Generating Invoice...");

                var folderPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Invoices");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var fileName = $"Invoice_Order_{order.Id}.txt";

                var fullPath = Path.Combine(folderPath, fileName);

              
                var invoiceContent =
        $@"===== INVOICE =====

Order Id: {order.Id}

User Id: {order.UserId}

Product Id: {order.ProductId}

Quantity: {order.Quantity}

Total Price: {order.TotalPrice}

Date: {DateTime.Now}

===================";
              
                await File.WriteAllTextAsync(fullPath, invoiceContent);

                Console.WriteLine($"Invoice Created: {fileName}");

              
                var repo = sp.GetRequiredService<IOrderRepository>();

                order.Status = "Completed";

                await repo.UpdateOrder(order);

                Console.WriteLine("Order marked as Completed");

                Console.WriteLine("BACKGROUND DONE");
            });

            return "Order Accepted ✔ (Processing in background)";
        }


        public async Task<string> BuyProduct_Blocking(int UserId, int ProductId, int quantity)
        {
            Console.WriteLine("BLOCKING START");

            var product = await _repo.GetProductById(ProductId);

            if (product.Stock < quantity)
                return "Failed: Not enough stock";

            await Task.Delay(5000);

          
            product.Stock -= quantity;

            await _repo.UpdateStockForProduct(product);

        
            var order = new Order
            {
                UserId = UserId,
                ProductId = ProductId,
                Quantity = quantity,
                OrderDate = DateTime.Now,
                TotalPrice = product.Price * quantity,
                Status = "Completed"
            };

            await _orderRepo.AddOrder(order);

            Console.WriteLine("Generating Invoice...");

            
            var folderPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Invoices");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            
            var fileName = $"Blocking_Invoice_Order_{order.Id}.txt";

            var fullPath = Path.Combine(folderPath, fileName);

           
            var invoiceContent =
        $@"===== BLOCKING INVOICE =====

Order Id: {order.Id}

User Id: {order.UserId}

Product Id: {order.ProductId}

Quantity: {order.Quantity}

Total Price: {order.TotalPrice}

Date: {DateTime.Now}

==============================";


            await File.WriteAllTextAsync(fullPath, invoiceContent);

            Console.WriteLine($"Invoice Created: {fileName}");

            Console.WriteLine("BLOCKING DONE");

            return "Order Completed Successfully (BLOCKING)";
        }
    }
}
