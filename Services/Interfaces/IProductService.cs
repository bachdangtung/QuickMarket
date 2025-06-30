using BussinessLogic.Models;
using Microsoft.AspNetCore.Http;

namespace Services.Interfaces
{
    public interface IProductService
    {
        Task<List<Product>> GetAllProductsAsync();
        Task<PagedResult<Product>> GetFilteredProductsAsync(ProductFilterDto filter);
        Task<Product?> GetProductByIdAsync(int productId);
        Task<List<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<List<Product>> GetProductsByUserAsync(int userId);
        Task<PagedResult<Product>> GetProductsByUserPagedAsync(int userId, int page, int pageSize);
        Task<bool> CreateProductAsync(Product product);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int productId);
        Task<List<ProductCategory>> GetAllCategoriesAsync();
        Task<ProductCategory?> GetCategoryByIdAsync(int categoryId);
        Task<string> UploadProductImageAsync(IFormFile imageFile);
        Task<bool> DeleteProductImageAsync(string imageUrl);
    }
}
