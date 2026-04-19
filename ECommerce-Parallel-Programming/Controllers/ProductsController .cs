using Ecommerce.Application.Use_Cases;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce_Parallel_Programming.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly GetProductsUseCase _useCase;
        private readonly BuyProductUseCase _use;
        public ProductsController(GetProductsUseCase useCase , BuyProductUseCase use)
        {
            _useCase = useCase;
            _use = use;
        }
        [HttpGet]
        public async Task<IActionResult> GetProducts() {
            var products = await _useCase.GetAllProducts();
            return Ok(products);
        }

        [HttpPost]
        public async Task <IActionResult> BuyProduct(int ProductId ,int UserId ,int quantity) {
            await _use.BuyProduct(UserId, ProductId, quantity);
            return Ok(200);
        }
    }
}
