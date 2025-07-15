using BussinessLogic.DTOs;
using BussinessLogic.Models;

namespace Repositories.Interfaces
{
    public interface IProductRepository
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
        Task<ProductImage?> GetProductImageByIdAsync(int imageId);
    }
}
