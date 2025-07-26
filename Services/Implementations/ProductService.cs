using AutoMapper;
using BussinessLogic.DTOs.Categories;
using BussinessLogic.DTOs.Products;
using BussinessLogic.Models;
using BussinessLogic.Models.Enums;
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

        public async Task<(List<ProductDto> Items, int TotalCount, int PageCount, int CurrentPage, int PageSize)> GetFilteredProductsAsync(ProductFilterDto filter)
        {
            // Get data from repository
            var pagedResult = await _productRepository.GetFilteredProductsAsync(filter);
            
            // Map to DTOs
            var productDtos = _mapper.Map<List<ProductDto>>(pagedResult.Items);
            
            // Return tuple with all paging info
            return (
                Items: productDtos,
                TotalCount: pagedResult.TotalCount,
                PageCount: pagedResult.PageCount,
                CurrentPage: pagedResult.CurrentPage,
                PageSize: pagedResult.PageSize
            );
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

        public async Task<(List<ProductDto> Items, int TotalCount, int PageCount, int CurrentPage, int PageSize)> GetProductsByUserPagedAsync(int userId, int page, int pageSize)
        {
            var pagedResult = await _productRepository.GetProductsByUserPagedAsync(userId, page, pageSize);
            var productDtos = _mapper.Map<List<ProductDto>>(pagedResult.Items);

            return (
                Items: productDtos,
                TotalCount: pagedResult.TotalCount,
                PageCount: pagedResult.PageCount,
                CurrentPage: pagedResult.CurrentPage,
                PageSize: pagedResult.PageSize
            );
        }

        public async Task<ProductDto?> GetProductByIdWithNestedReviewsAsync(int productId)
        {
            var product = await _productRepository.GetProductByIdAsync(productId);
            if (product == null) return null;
            
            var productDto = _mapper.Map<ProductDto>(product);
            
            // Initialize if null
            if (productDto.Reviews == null)
            {
                productDto.Reviews = new List<ProductReviewDto>();
                System.Diagnostics.Debug.WriteLine("Reviews collection was null, initialized empty list");
            }
            
            // Tách riêng reviews gốc (không có ThreadId) và replies (có ThreadId)
            var mainReviews = productDto.Reviews.Where(r => r.ThreadId == null).ToList();
            var replies = productDto.Reviews.Where(r => r.ThreadId != null).ToList();
            
            // Gán replies vào review gốc tương ứng
            foreach (var reply in replies)
            {
                var parentReview = productDto.Reviews.FirstOrDefault(r => r.ReviewId == reply.ThreadId);
                if (parentReview != null)
                {
                    if (parentReview.Replies == null)
                        parentReview.Replies = new ProductReviewDto[] { };
                    
                    var replyList = parentReview.Replies.ToList();
                    replyList.Add(reply);
                    parentReview.Replies = replyList.ToArray();
                }
            }

            // Chỉ giữ lại các reviews gốc (các replies đã được gắn vào reviews gốc)
            productDto.Reviews = mainReviews;
            
            return productDto;
        }

        public async Task<(bool Success, int ProductId, string? ErrorMessage)> CreateProductAsync(ProductDto productDto)
        {
            try
            {
                // Map DTO to entity
                var product = _mapper.Map<Product>(productDto);
                
                // Set date posted to current date/time
                product.DatePosted = DateTime.Now;
                
                // Set default status if not provided
                if (string.IsNullOrEmpty(product.Status))
                {
                    product.Status = ProductStatus.Active.ToString();
                }

                var success = await _productRepository.CreateProductAsync(product);
                
                if (success)
                {
                    return (Success: true, ProductId: product.ProductId, ErrorMessage: null);
                }
                else
                {
                    return (Success: false, ProductId: 0, ErrorMessage: "Failed to create product");
                }
            }
            catch (Exception ex)
            {
                return (Success: false, ProductId: 0, ErrorMessage: $"Error creating product: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateProductAsync(ProductDto productDto)
        {
            try
            {
                // Lấy sản phẩm hiện có từ cơ sở dữ liệu
                var existingProduct = await _productRepository.GetProductByIdAsync(productDto.ProductId);
                if (existingProduct == null)
                    return (Success: false, ErrorMessage: "Product not found");

                // Cập nhật các thuộc tính của sản phẩm
                existingProduct.Name = productDto.Name;
                existingProduct.Description = productDto.Description;
                existingProduct.Price = productDto.Price;
                existingProduct.Status = productDto.Status;
                existingProduct.CategoryId = productDto.CategoryId;

                // Không cập nhật các thuộc tính quan hệ trực tiếp từ DTO
                // Các thuộc tính như Images, Reviews... cần được xử lý riêng biệt

                var success = await _productRepository.UpdateProductAsync(existingProduct);
                
                if (success)
                {
                    return (Success: true, ErrorMessage: null);
                }
                else
                {
                    return (Success: false, ErrorMessage: "Failed to update product");
                }
            }
            catch (Exception ex)
            {
                return (Success: false, ErrorMessage: $"Error updating product: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteProductAsync(int productId)
        {
            try
            {
                var success = await _productRepository.DeleteProductAsync(productId);
                
                if (success)
                {
                    return (Success: true, ErrorMessage: null);
                }
                else
                {
                    return (Success: false, ErrorMessage: "Failed to delete product");
                }
            }
            catch (Exception ex)
            {
                return (Success: false, ErrorMessage: $"Error deleting product: {ex.Message}");
            }
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
                return string.Empty;
                
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
        
        public async Task<(bool Success, string? ErrorMessage)> AddProductReviewAsync(int productId, int userId, byte rating, string comment, int? threadId = null)
        {
            try
            {
                // Validate the rating - allow 0 for comments without rating or replies
                if (rating != 0 && (rating < 1 || rating > 5))
                {
                    return (Success: false, ErrorMessage: "Rating must be between 1 and 5 stars");
                }

                // Get the product
                var product = await _productRepository.GetProductByIdAsync(productId);
                if (product == null)
                {
                    return (Success: false, ErrorMessage: "Product not found");
                }

                // If this is a root review (not a reply)
                if (!threadId.HasValue)
                {
                    // Only restrict if user is trying to add a review with rating (stars)
                    // Allow multiple comments (rating=0) from the same user
                    if (rating > 0 && product.ProductReviews.Any(r => r.UserId == userId && r.ThreadId == null && r.Rating > 0))
                    {
                        return (Success: false, ErrorMessage: "You have already rated this product with stars");
                    }

                    // Check if user is reviewing their own product
                    if (product.UserId == userId)
                    {
                        return (Success: false, ErrorMessage: "You cannot review your own product");
                    }
                }

                // Add debug info
                System.Diagnostics.Debug.WriteLine($"Creating review: ProductId={productId}, UserId={userId}, Rating={rating}, Comment={comment}, ThreadId={threadId}");
                
                // Create the review
                var review = new ProductReview
                {
                    ProductId = productId,
                    UserId = userId,
                    Rating = rating,
                    Comment = comment,
                    ReviewDate = DateTime.Now,
                    ThreadId = threadId
                };

                // Create a separate review instead of adding to the product collection
                // This avoids potential EF tracking issues
                
                // Debug info before saving
                System.Diagnostics.Debug.WriteLine($"Creating a new review with ProductId: {productId}, UserId: {userId}");
                
                // Save the review directly to ensure it's properly saved and tracked
                var result = await _productRepository.AddProductReviewAsync(review);

                if (result)
                {
                    return (Success: true, ErrorMessage: null);
                }
                else
                {
                    return (Success: false, ErrorMessage: "Failed to add review");
                }
            }
            catch (Exception ex)
            {
                return (Success: false, ErrorMessage: $"Error adding review: {ex.Message}");
            }
        }
        
        // Favorite methods implementation
        public async Task<(bool Success, string? ErrorMessage)> AddFavoriteAsync(int userId, int productId)
        {
            try
            {
                var result = await _productRepository.AddFavoriteAsync(userId, productId);
                return result 
                    ? (Success: true, ErrorMessage: null) 
                    : (Success: false, ErrorMessage: "Failed to add favorite");
            }
            catch (Exception ex)
            {
                return (Success: false, ErrorMessage: $"Error adding favorite: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> RemoveFavoriteAsync(int userId, int productId)
        {
            try
            {
                var result = await _productRepository.RemoveFavoriteAsync(userId, productId);
                return result 
                    ? (Success: true, ErrorMessage: null) 
                    : (Success: false, ErrorMessage: "Failed to remove favorite");
            }
            catch (Exception ex)
            {
                return (Success: false, ErrorMessage: $"Error removing favorite: {ex.Message}");
            }
        }

        public async Task<bool> IsFavoriteAsync(int userId, int productId)
        {
            return await _productRepository.IsFavoriteAsync(userId, productId);
        }

        public async Task<List<ProductDto>> GetUserFavoritesAsync(int userId)
        {
            var favoriteProducts = await _productRepository.GetUserFavoritesAsync(userId);
            return _mapper.Map<List<ProductDto>>(favoriteProducts);
        }

        public async Task<(List<ProductDto> Items, int TotalCount, int PageCount, int CurrentPage, int PageSize)> GetUserFavoritesPagedAsync(int userId, int page, int pageSize, string sortOrder = "", int? categoryId = null)
        {
            var pagedResult = await _productRepository.GetUserFavoritesPagedAsync(userId, page, pageSize, sortOrder, categoryId);
            var productDtos = _mapper.Map<List<ProductDto>>(pagedResult.Items);

            return (
                Items: productDtos,
                TotalCount: pagedResult.TotalCount,
                PageCount: pagedResult.PageCount,
                CurrentPage: pagedResult.CurrentPage,
                PageSize: pagedResult.PageSize
            );
        }
        
        public async Task<bool> AddProductImageAsync(int productId, string imageUrl)
        {
            return await _productRepository.AddProductImageAsync(productId, imageUrl);
        }
        
        public async Task<bool> HasUserPurchasedProductAsync(int userId, int productId)
        {
            // Kiểm tra xem người dùng đã mua sản phẩm này chưa
            // Dựa vào bảng Transaction để kiểm tra
            return await _productRepository.HasUserPurchasedProductAsync(userId, productId);
        }
    }
}
