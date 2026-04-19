using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Use_Cases
{
    public class BuyProductUseCase
    {
        private readonly IProductRepository _repo;
        private static readonly object _lock = new object();
        private readonly IOrderRepository _orderRepo;

        public BuyProductUseCase(IProductRepository repo , IOrderRepository orderRepo)
        {
            _repo = repo;
            _orderRepo = orderRepo;
        }
        public async Task BuyProduct(int UserId,int ProductId ,int quantity) {
            lock (_lock) { 
            var product = _repo.GetProductById(ProductId).GetAwaiter().GetResult();
                if (product.Stock >= quantity) {
                    product.Stock -= quantity;
                    _repo.UpdateStockForProduct(product).GetAwaiter().GetResult();

                    var Order = new Order
                    {
                        UserId = UserId,
                        ProductId = ProductId,
                        Quantity = quantity,
                        OrderDate= DateTime.Now ,
                        TotalPrice = product.Price * quantity,
                    };
                    _orderRepo.AddOrder(Order).GetAwaiter().GetResult();
                }

            }
        }

    }
}
