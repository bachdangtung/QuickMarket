using BussinessLogic.DTOs;
using BussinessLogic.DTOs.Categories;
using BussinessLogic.DTOs.Products;
using BussinessLogic.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;
using AutoMapper;

namespace QuickMarket.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IMapper _mapper;

        public ProductController(IProductService productService, IMapper mapper)
        {
            _productService = productService;
            _mapper = mapper;
        }

        // GET: Product
        public async Task<IActionResult> Index(string? searchQuery = null, int? categoryId = null, string? sortOrder = null, int page = 1)
        {
            var pageSize = 12; // Số sản phẩm trên mỗi trang
            
            // Tạo DTO chứa các tham số lọc và phân trang
            var filter = new ProductFilterDto
            {
                SearchQuery = searchQuery ?? string.Empty,
                CategoryId = categoryId,
                SortOrder = sortOrder ?? string.Empty,
                Page = page,
                PageSize = pageSize
            };
            
            // Gọi service để lọc, sắp xếp và phân trang ngay từ database
            var pagedResult = await _productService.GetFilteredProductsAsync(filter);
            
            var categories = await _productService.GetAllCategoriesAsync();

            var productListDto = new ProductListDto
            {
                Products = pagedResult.Items,
                Categories = categories,  
                SearchQuery = searchQuery,
                CategoryFilter = categoryId,
                SortOrder = sortOrder,
                CurrentPage = pagedResult.CurrentPage,
                TotalPages = pagedResult.PageCount
            };

            return View(productListDto);
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var productDto = await _productService.GetProductByIdAsync(id);
            if (productDto == null)
            {
                return NotFound();
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

            // Convert ProductDto to ProductCreateUpdateDto for the view
            var createUpdateDto = _mapper.Map<ProductCreateUpdateDto>(productDto);
            
            return View(createUpdateDto);
        }

        // GET: Product/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _productService.GetAllCategoriesAsync();
            return View(new ProductCreateUpdateDto { DatePosted = DateTime.Now, Status = "Active" });
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(ProductCreateUpdateDto productDto, List<IFormFile> imageFiles)
        {
            if (ModelState.IsValid)
            {
                // Set up user ID and other defaults
                productDto.DatePosted = DateTime.Now;
                productDto.Status = "Active";
                productDto.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                // Convert to ProductDto for service call
                var serviceProductDto = _mapper.Map<ProductDto>(productDto);
                var result = await _productService.CreateProductAsync(serviceProductDto);
                
                if (result)
                {
                    // Xử lý tải lên hình ảnh
                    if (imageFiles != null && imageFiles.Count > 0)
                    {
                        await SaveProductImages(serviceProductDto.ProductId, imageFiles);
                    }

                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.Categories = await _productService.GetAllCategoriesAsync();
            return View(productDto);
        }

        // GET: Product/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var productDto = await _productService.GetProductByIdAsync(id);
            if (productDto == null)
            {
                return NotFound();
            }

            // Kiểm tra xem người dùng hiện tại có phải là chủ sở hữu hoặc admin không
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (productDto.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var editDto = _mapper.Map<ProductCreateUpdateDto>(productDto);

            ViewBag.Categories = await _productService.GetAllCategoriesAsync();
            return View(editDto);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, ProductCreateUpdateDto productDto, List<IFormFile> imageFiles)
        {
            if (id != productDto.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingProductDto = await _productService.GetProductByIdAsync(id);
                if (existingProductDto == null)
                {
                    return NotFound();
                }

                // Kiểm tra xem người dùng hiện tại có phải là chủ sở hữu hoặc admin không
                var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (existingProductDto.UserId != currentUserId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                // Cập nhật thông tin sản phẩm từ ProductCreateUpdateDto vào DTO
                var updatedProductDto = _mapper.Map<ProductDto>(productDto);
                updatedProductDto.ProductId = id;
                
                // Giữ nguyên thông tin không thay đổi
                updatedProductDto.UserId = existingProductDto.UserId;
                updatedProductDto.DatePosted = existingProductDto.DatePosted;
                updatedProductDto.ImageUrls = existingProductDto.ImageUrls;

                var result = await _productService.UpdateProductAsync(updatedProductDto);
                
                if (result)
                {
                    // Xử lý tải lên hình ảnh
                    if (imageFiles != null && imageFiles.Count > 0)
                    {
                        await SaveProductImages(updatedProductDto.ProductId, imageFiles);
                    }

                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.Categories = await _productService.GetAllCategoriesAsync();
            return View(productDto);
        }

        // GET: Product/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var productDto = await _productService.GetProductByIdAsync(id);
            if (productDto == null)
            {
                return NotFound();
            }

            // Kiểm tra xem người dùng hiện tại có phải là chủ sở hữu hoặc admin không
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (productDto.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Convert ProductDto to ProductCreateUpdateDto for the view
            var createUpdateDto = _mapper.Map<ProductCreateUpdateDto>(productDto);

            return View(createUpdateDto);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Kiểm tra xem người dùng hiện tại có phải là chủ sở hữu hoặc admin không
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (product.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            await _productService.DeleteProductAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Product/ManageProducts
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ManageProducts(string? searchQuery = null, int? categoryId = null, string? sortOrder = null, int page = 1)
        {
            var pageSize = 20; // Hiển thị nhiều sản phẩm hơn trong chế độ quản lý
            
            // Tạo DTO chứa các tham số lọc và phân trang
            var filter = new ProductFilterDto
            {
                SearchQuery = searchQuery,
                CategoryId = categoryId,
                SortOrder = sortOrder,
                Page = page,
                PageSize = pageSize
            };
            
            // Gọi service để lọc, sắp xếp và phân trang ngay từ database
            var pagedResult = await _productService.GetFilteredProductsAsync(filter);
            
            var categories = await _productService.GetAllCategoriesAsync();

            var productListDto = new ProductListDto
            {
                Products = pagedResult.Items,
                Categories = categories,
                SearchQuery = searchQuery,
                CategoryFilter = categoryId,
                SortOrder = sortOrder,
                CurrentPage = pagedResult.CurrentPage,
                TotalPages = pagedResult.PageCount
            };

            return View(productListDto);
        }

        // GET: Product/MyProducts
        [Authorize]
        public async Task<IActionResult> MyProducts(int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var pageSize = 10; // Số sản phẩm trên mỗi trang
            
            // Sử dụng phiên bản có phân trang
            var pagedResult = await _productService.GetProductsByUserPagedAsync(userId, page, pageSize);
            
            // Tạo một productListDto để bao gồm thông tin phân trang
            var productListDto = new ProductListDto
            {
                Products = pagedResult.Items,
                CurrentPage = pagedResult.CurrentPage,
                TotalPages = pagedResult.PageCount
            };
            
            return View(productListDto);
        }

        // POST: Product/AddReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddReview(int productId, byte rating, string comment, int? threadId = null)
        {
            if (rating < 1 || rating > 5)
            {
                ModelState.AddModelError("", "Đánh giá phải từ 1 đến 5 sao");
                return RedirectToAction(nameof(Details), new { id = productId });
            }

            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            
            // Nếu là bình luận gốc (không phải trả lời)
            if (!threadId.HasValue)
            {
                // Kiểm tra xem người dùng đã đánh giá sản phẩm này chưa (chỉ với bình luận gốc, không phải reply)
                if (product.Reviews.Any(r => r.UserId == currentUserId && r.ThreadId == null))
                {
                    ModelState.AddModelError("", "Bạn đã đánh giá sản phẩm này rồi");
                    return RedirectToAction(nameof(Details), new { id = productId });
                }

                // Kiểm tra xem người dùng có đánh giá sản phẩm của chính mình không
                if (product.UserId == currentUserId)
                {
                    ModelState.AddModelError("", "Bạn không thể đánh giá sản phẩm của chính mình");
                    return RedirectToAction(nameof(Details), new { id = productId });
                }
            }

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = currentUserId,
                Rating = rating,
                Comment = comment,
                ReviewDate = DateTime.Now,
                ThreadId = threadId
            };

            // Create a new ProductReviewDto to add to the product's Reviews collection
            var reviewDto = new ProductReviewDto
            {
                ReviewId = review.ReviewId,
                ProductId = productId,
                UserId = currentUserId,
                Rating = rating,
                Comment = comment,
                ReviewDate = DateTime.Now,
                ThreadId = threadId
            };

            // Thêm đánh giá vào sản phẩm
            product.Reviews.Add(reviewDto);
            await _productService.UpdateProductAsync(product);

            return RedirectToAction(nameof(Details), new { id = productId });
        }

        // Phương thức hỗ trợ lưu hình ảnh sản phẩm
        private async Task SaveProductImages(int productId, List<IFormFile> imageFiles)
        {
            // Lấy thông tin sản phẩm
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
                return;

            foreach (var imageFile in imageFiles)
            {
                if (imageFile.Length > 0)
                {
                    // Upload image to Cloudinary
                    var imageUrl = await _productService.UploadProductImageAsync(imageFile);
                    
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        // Since ProductDto doesn't have a ProductImages collection, just add to ImageUrls
                        product.ImageUrls.Add(imageUrl);
                        
                        // If you need to also create a ProductImage entity, you'll need to handle it separately
                        // through the service layer
                    }
                }
            }

            await _productService.UpdateProductAsync(product);
        }

        // POST: /Product/DeleteImage
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            // First find which product has this image - we need to refactor this approach
            // since ProductDto doesn't have direct access to image IDs
            
            // Instead, we should get the image information from a dedicated service method
            var imageDetails = await _productService.GetImageDetailsAsync(imageId);
            if (imageDetails == null)
            {
                return Json(new { success = false, message = "Image not found" });
            }
            
            var productId = imageDetails.ProductId;
            var imageUrl = imageDetails.ImageUrl;
            
            // Get the product
            var product = await _productService.GetProductByIdAsync(productId);
            
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found for this image" });
            }
            
            // Verify the user owns the product
            if (product.UserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0"))
            {
                return Json(new { success = false, message = "You are not authorized to delete this image" });
            }
            
            // Delete from Cloudinary
            var cloudDeleteResult = await _productService.DeleteProductImageAsync(imageUrl);
            if (!cloudDeleteResult)
            {
                return Json(new { success = false, message = "Failed to delete image from cloud storage" });
            }
            
            // Remove from product's image URLs collection
            product.ImageUrls.Remove(imageUrl);
            
            // Update product
            var updateResult = await _productService.UpdateProductAsync(product);
            if (!updateResult)
            {
                return Json(new { success = false, message = "Failed to update product" });
            }
            
            return Json(new { success = true });
        }

        // POST: Product/UpdateStatus
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int productId, string status)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            
            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
            }

            if (string.IsNullOrEmpty(status) || !new[] { "Active", "Inactive", "Sold" }.Contains(status))
            {
                return Json(new { success = false, message = "Trạng thái không hợp lệ" });
            }

            product.Status = status;
            var result = await _productService.UpdateProductAsync(product);

            if (!result)
            {
                return Json(new { success = false, message = "Không thể cập nhật trạng thái sản phẩm" });
            }

            return Json(new { success = true });
        }
    }
}
