using BussinessLogic.DTOs;
using BussinessLogic.DTOs.Products;
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
        
        // Favorite methods
        Task<bool> AddFavoriteAsync(int userId, int productId);
        Task<bool> RemoveFavoriteAsync(int userId, int productId);
        Task<bool> IsFavoriteAsync(int userId, int productId);
        Task<List<Product>> GetUserFavoritesAsync(int userId);
        Task<PagedResult<Product>> GetUserFavoritesPagedAsync(int userId, int page, int pageSize);
    }
}
