using Ecommerce.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ecommerce.Domain.Entities;
using System.ComponentModel;

namespace Ecommerce.Application.Use_Cases
{
    public class GetProductsUseCase
    {
        private readonly IProductRepository _repo;
        public GetProductsUseCase(IProductRepository repo)
        {
            _repo = repo;
        }
        public async Task<List<Product>> GetAllProducts() { 
        return await _repo.GetProducts();
        }
    }
}
