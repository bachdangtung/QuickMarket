using AutoMapper;
using BussinessLogic.DTOs;
using BussinessLogic.DTOs.Categories;
using BussinessLogic.DTOs.Products;
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
        private readonly IMapper _mapper;

        public ProductService(IProductRepository productRepository, ICloudinaryService cloudinaryService, IMapper mapper)
        {
            _productRepository = productRepository;
            _cloudinaryService = cloudinaryService; 
            _mapper = mapper;
        }

        public async Task<List<ProductDto>> GetAllProductsAsync()
        {
            var products = await _productRepository.GetAllProductsAsync();
            return _mapper.Map<List<ProductDto>>(products);
        }

        public async Task<PagedResult<ProductDto>> GetFilteredProductsAsync(ProductFilterDto filter)
        {
            // Get data from repository
            var pagedResult = await _productRepository.GetFilteredProductsAsync(filter);
            
            // Map to DTOs
            var productDtos = _mapper.Map<List<ProductDto>>(pagedResult.Items);
            
            // Return a new PagedResult with mapped items
            return new PagedResult<ProductDto>
            {
                Items = productDtos,
                TotalCount = pagedResult.TotalCount,
                PageCount = pagedResult.PageCount,
                CurrentPage = pagedResult.CurrentPage,
                PageSize = pagedResult.PageSize
            };
        }

        public async Task<ProductDto?> GetProductByIdAsync(int productId)
        {
            var product = await _productRepository.GetProductByIdAsync(productId);
            return product == null ? null : _mapper.Map<ProductDto>(product);
        }

        public async Task<List<ProductDto>> GetProductsByCategoryAsync(int categoryId)
        {
            var products = await _productRepository.GetProductsByCategoryAsync(categoryId);
            return _mapper.Map<List<ProductDto>>(products);
        }

        public async Task<List<ProductDto>> GetProductsByUserAsync(int userId)
        {
            var products = await _productRepository.GetProductsByUserAsync(userId);
            return _mapper.Map<List<ProductDto>>(products);
        }

        public async Task<PagedResult<ProductDto>> GetProductsByUserPagedAsync(int userId, int page, int pageSize)
        {
            var pagedResult = await _productRepository.GetProductsByUserPagedAsync(userId, page, pageSize);
            var productDtos = _mapper.Map<List<ProductDto>>(pagedResult.Items);

            return new PagedResult<ProductDto>
            {
                Items = productDtos,
                TotalCount = pagedResult.TotalCount,
                PageCount = pagedResult.PageCount,
                CurrentPage = pagedResult.CurrentPage,
                PageSize = pagedResult.PageSize
            };
        }

        public async Task<bool> CreateProductAsync(ProductDto productDto)
        {
            // Map DTO to entity
            var product = _mapper.Map<Product>(productDto);
            
            // Set date posted to current date/time
            product.DatePosted = DateTime.Now;
            
            // Set default status if not provided
            if (string.IsNullOrEmpty(product.Status))
            {
                product.Status = "Active";
            }

            return await _productRepository.CreateProductAsync(product);
        }

        public async Task<bool> UpdateProductAsync(ProductDto productDto)
        {
            // Lấy sản phẩm hiện có từ cơ sở dữ liệu
            var existingProduct = await _productRepository.GetProductByIdAsync(productDto.ProductId);
            if (existingProduct == null)
                return false;

            // Cập nhật các thuộc tính của sản phẩm
            existingProduct.Name = productDto.Name;
            existingProduct.Description = productDto.Description;
            existingProduct.Price = productDto.Price;
            existingProduct.Status = productDto.Status;
            existingProduct.CategoryId = productDto.CategoryId;

            // Không cập nhật các thuộc tính quan hệ trực tiếp từ DTO
            // Các thuộc tính như Images, Reviews... cần được xử lý riêng biệt

            return await _productRepository.UpdateProductAsync(existingProduct);
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            return await _productRepository.DeleteProductAsync(productId);
        }

        public async Task<List<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _productRepository.GetAllCategoriesAsync();
            return _mapper.Map<List<CategoryDto>>(categories);
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int categoryId)
        {
            var category = await _productRepository.GetCategoryByIdAsync(categoryId);
            return category == null ? null : _mapper.Map<CategoryDto>(category);
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

        public async Task<ProductImageDto?> GetImageDetailsAsync(int imageId)
        {
            // We need to look up the image by its ID
            // This will depend on how your repository is structured
            var image = await _productRepository.GetProductImageByIdAsync(imageId);
            if (image == null)
                return null;

            return _mapper.Map<ProductImageDto>(image);
        }
    }
}
