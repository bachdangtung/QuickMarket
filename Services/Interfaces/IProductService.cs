using BussinessLogic.DTOs;
using BussinessLogic.DTOs.Categories;
using BussinessLogic.DTOs.Products;
using Microsoft.AspNetCore.Http;

namespace Services.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetAllProductsAsync();
        Task<PagedResult<ProductDto>> GetFilteredProductsAsync(ProductFilterDto filter);
        Task<ProductDto?> GetProductByIdAsync(int productId);
        Task<List<ProductDto>> GetProductsByCategoryAsync(int categoryId);
        Task<List<ProductDto>> GetProductsByUserAsync(int userId);
        Task<PagedResult<ProductDto>> GetProductsByUserPagedAsync(int userId, int page, int pageSize);
        Task<bool> CreateProductAsync(ProductDto productDto);
        Task<bool> UpdateProductAsync(ProductDto productDto);
        Task<bool> DeleteProductAsync(int productId);
        Task<List<CategoryDto>> GetAllCategoriesAsync();
        Task<CategoryDto?> GetCategoryByIdAsync(int categoryId);
        Task<string> UploadProductImageAsync(IFormFile imageFile);
        Task<bool> DeleteProductImageAsync(string imageUrl);
        Task<ProductImageDto?> GetImageDetailsAsync(int imageId);
    }
}
