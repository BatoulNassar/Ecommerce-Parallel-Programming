using Ecommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Product>> GetProducts();
        Task UpdateStockForProduct (Product product);
        Task<Product> GetProductById(int id);
    }
}
