using BussinessLogic.Models;
using Microsoft.AspNetCore.Http;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICloudinaryService _cloudinaryService;

        public ProductService(IProductRepository productRepository, ICloudinaryService cloudinaryService)
        {
            _productRepository = productRepository;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _productRepository.GetAllProductsAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _productRepository.GetProductByIdAsync(productId);
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _productRepository.GetProductsByCategoryAsync(categoryId);
        }

        public async Task<List<Product>> GetProductsByUserAsync(int userId)
        {
            return await _productRepository.GetProductsByUserAsync(userId);
        }

        public async Task<bool> CreateProductAsync(Product product)
        {
            // Set date posted to current date/time
            product.DatePosted = DateTime.Now;
            
            // Set default status if not provided
            if (string.IsNullOrEmpty(product.Status))
            {
                product.Status = "Active";
            }

            return await _productRepository.CreateProductAsync(product);
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            return await _productRepository.UpdateProductAsync(product);
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            return await _productRepository.DeleteProductAsync(productId);
        }

        public async Task<List<ProductCategory>> GetAllCategoriesAsync()
        {
            return await _productRepository.GetAllCategoriesAsync();
        }

        public async Task<ProductCategory?> GetCategoryByIdAsync(int categoryId)
        {
            return await _productRepository.GetCategoryByIdAsync(categoryId);
        }

        public async Task<string> UploadProductImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;
                
            return await _cloudinaryService.UploadImageAsync(imageFile);
        }
        
        public async Task<bool> DeleteProductImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return false;
                
            // Extract public_id if it's a Cloudinary URL
            if (imageUrl.Contains("cloudinary.com"))
            {
                return await _cloudinaryService.DeleteImageAsync(imageUrl);
            }
            
            // For local files, we would need to delete them from the disk
            // But since we're moving away from local storage, we'll return true
            return true;
        }
    }
}
