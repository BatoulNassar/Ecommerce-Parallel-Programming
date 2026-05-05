using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Application.Use_Cases
{
    public class BuyProductUseCase
    {
        private readonly IProductRepository _repo;
        private readonly IOrderRepository _orderRepo;


        private static readonly int _coreCount = Environment.ProcessorCount;

        private static readonly int _maxConcurrency = _coreCount * (1 + 4);

        private static readonly SemaphoreSlim _capacityThrottle = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);

        private static readonly SemaphoreSlim _asyncLock = new SemaphoreSlim(1, 1);

        public BuyProductUseCase(IProductRepository repo, IOrderRepository orderRepo)
        {
            _repo = repo;
            _orderRepo = orderRepo;
        }

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

        public async Task BuyProductWithoutThread(int UserId, int ProductId, int quantity)
        {
            var product = await _repo.GetProductById(ProductId);

            if (product.Stock >= quantity)
            {
                await Task.Delay(100);
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
    }
}