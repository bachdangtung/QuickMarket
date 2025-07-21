using BussinessLogic.DTOs;
using BussinessLogic.DTOs.Categories;
using BussinessLogic.DTOs.Products;
using BussinessLogic.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetAllProductsAsync();
        Task<(List<ProductDto> Items, int TotalCount, int PageCount, int CurrentPage, int PageSize)> GetFilteredProductsAsync(ProductFilterDto filter);
        Task<ProductDto?> GetProductByIdAsync(int productId);
        Task<ProductDto?> GetProductByIdWithNestedReviewsAsync(int productId);
        Task<List<ProductDto>> GetProductsByCategoryAsync(int categoryId);
        Task<List<ProductDto>> GetProductsByUserAsync(int userId);
        Task<(List<ProductDto> Items, int TotalCount, int PageCount, int CurrentPage, int PageSize)> GetProductsByUserPagedAsync(int userId, int page, int pageSize);
        Task<(bool Success, int ProductId, string? ErrorMessage)> CreateProductAsync(ProductDto productDto);
        Task<(bool Success, string? ErrorMessage)> UpdateProductAsync(ProductDto productDto);
        Task<(bool Success, string? ErrorMessage)> DeleteProductAsync(int productId);
        Task<List<CategoryDto>> GetAllCategoriesAsync();
        Task<CategoryDto?> GetCategoryByIdAsync(int categoryId);
        Task<string> UploadProductImageAsync(IFormFile imageFile);
        Task<bool> DeleteProductImageAsync(string imageUrl);
        Task<ProductImageDto?> GetImageDetailsAsync(int imageId);
        Task<(bool Success, string? ErrorMessage)> AddProductReviewAsync(int productId, int userId, byte rating, string comment, int? threadId = null);
    }
}
