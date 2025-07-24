using AutoMapper;
using BussinessLogic.DTOs.Products;
using BussinessLogic.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;

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
        // Tất cả người dùng đều có thể xem danh sách sản phẩm
        [AllowAnonymous]
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
            var (items, totalCount, pageCount, currentPage, _) = await _productService.GetFilteredProductsAsync(filter);
            
            var categories = await _productService.GetAllCategoriesAsync();

            var productListDto = new ProductListDto
            {
                Products = items,
                Categories = categories,  
                SearchQuery = searchQuery,
                CategoryFilter = categoryId,
                SortOrder = sortOrder,
                CurrentPage = currentPage,
                TotalPages = pageCount
            };

            return View(productListDto);
        }

        // GET: Product/Details/5
        // Tất cả người dùng đều có thể xem chi tiết sản phẩm
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            // Sử dụng phương thức mới từ service để lấy sản phẩm với reviews đã được tổ chức
            var productDto = await _productService.GetProductByIdWithNestedReviewsAsync(id);
            if (productDto == null)
            {
                return NotFound();
            }

            // Convert ProductDto to ProductCreateUpdateDto for the view
            var createUpdateDto = _mapper.Map<ProductCreateUpdateDto>(productDto);
            
            return View(createUpdateDto);
        }

        // GET: Product/Create
        // Chỉ người bán, người quản lý và admin mới có thể tạo sản phẩm
        [Authorize(Roles = "Admin,Manager,Seller")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _productService.GetAllCategoriesAsync();
            return View(new ProductCreateUpdateDto { DatePosted = DateTime.Now, Status = "Active" });
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Seller")]
        public async Task<IActionResult> Create(ProductCreateUpdateDto productDto, List<IFormFile> imageFiles)
        {
            if (ModelState.IsValid)
            {
                // Set up user ID and other defaults
                productDto.DatePosted = DateTime.Now;
                productDto.Status = ProductStatus.Active.ToString();
                productDto.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                // Convert to ProductDto for service call
                var serviceProductDto = _mapper.Map<ProductDto>(productDto);
                var (success, productId, errorMessage) = await _productService.CreateProductAsync(serviceProductDto);
                
                if (success)
                {
                    // Xử lý tải lên hình ảnh nếu sản phẩm được tạo thành công
                    if (imageFiles != null && imageFiles.Count > 0)
                    {
                        await SaveProductImages(productId, imageFiles);
                    }

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // Thêm lỗi từ service vào ModelState
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        ModelState.AddModelError("", errorMessage);
                    }
                }
            }

            ViewBag.Categories = await _productService.GetAllCategoriesAsync();
            return View(productDto);
        }

        // GET: Product/Edit/5
        [Authorize(Roles = "Admin,Manager,Seller")]
        public async Task<IActionResult> Edit(int id)
        {
            var productDto = await _productService.GetProductByIdAsync(id);
            if (productDto == null)
            {
                return NotFound();
            }

            // Kiểm tra xem người dùng hiện tại có phải là chủ sở hữu, manager hoặc admin không
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (productDto.UserId != currentUserId && !User.IsInRole("Admin") && !User.IsInRole("Manager"))
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
        [Authorize(Roles = "Admin,Manager,Seller")]
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

                // Kiểm tra xem người dùng hiện tại có phải là chủ sở hữu, manager hoặc admin không
                var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (existingProductDto.UserId != currentUserId && !User.IsInRole("Admin") && !User.IsInRole("Manager"))
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

                var (success, errorMessage) = await _productService.UpdateProductAsync(updatedProductDto);
                
                if (success)
                {
                    // Xử lý tải lên hình ảnh
                    if (imageFiles != null && imageFiles.Count > 0)
                    {
                        await SaveProductImages(updatedProductDto.ProductId, imageFiles);
                    }

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // Thêm lỗi từ service vào ModelState
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        ModelState.AddModelError("", errorMessage);
                    }
                }
            }

            ViewBag.Categories = await _productService.GetAllCategoriesAsync();
            return View(productDto);
        }

        // GET: Product/Delete/5
        [Authorize(Roles = "Admin,Manager,Seller")]
        public async Task<IActionResult> Delete(int id)
        {
            var productDto = await _productService.GetProductByIdAsync(id);
            if (productDto == null)
            {
                return NotFound();
            }

            // Kiểm tra xem người dùng hiện tại có phải là chủ sở hữu, manager hoặc admin không
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (productDto.UserId != currentUserId && !User.IsInRole("Admin") && !User.IsInRole("Manager"))
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
        [Authorize(Roles = "Admin,Manager,Seller")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Kiểm tra xem người dùng hiện tại có phải là chủ sở hữu, manager hoặc admin không
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (product.UserId != currentUserId && !User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                return Forbid();
            }

            var (success, errorMessage) = await _productService.DeleteProductAsync(id);
            
            if (!success)
            {
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    ModelState.AddModelError("", errorMessage);
                }
                return View(product);
            }
            
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
            var (items, totalCount, pageCount, currentPage, _) = await _productService.GetFilteredProductsAsync(filter);
            
            var categories = await _productService.GetAllCategoriesAsync();

            var productListDto = new ProductListDto
            {
                Products = items,
                Categories = categories,
                SearchQuery = searchQuery,
                CategoryFilter = categoryId,
                SortOrder = sortOrder,
                CurrentPage = currentPage,
                TotalPages = pageCount
            };

            return View(productListDto);
        }

        // GET: Product/MyProducts
        // Người bán xem sản phẩm của họ, Admin/Manager xem tất cả
        [Authorize(Roles = "Admin,Manager,Seller")]
        public async Task<IActionResult> MyProducts(int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var pageSize = 10; // Số sản phẩm trên mỗi trang
            
            // Sử dụng phiên bản có phân trang
            var (items, totalCount, pageCount, currentPage, _) = await _productService.GetProductsByUserPagedAsync(userId, page, pageSize);
            
            // Tạo một productListDto để bao gồm thông tin phân trang
            var productListDto = new ProductListDto
            {
                Products = items,
                CurrentPage = currentPage,
                TotalPages = pageCount
            };
            
            return View(productListDto);
        }

        // POST: Product/AddReview
        // Chỉ khách hàng đã mua hàng và admin/manager mới có thể đánh giá sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Buyer")]
        public async Task<IActionResult> AddReview(int productId, byte rating, string comment, int? threadId = null)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            
            // Sử dụng service để thêm đánh giá
            var (success, errorMessage) = await _productService.AddProductReviewAsync(productId, currentUserId, rating, comment, threadId);
            
            if (!success)
            {
                // Thêm lỗi vào ModelState
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    ModelState.AddModelError("", errorMessage);
                }
            }
            
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
        [Authorize(Roles = "Admin,Manager,Seller")]
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
            var (success, errorMessage) = await _productService.UpdateProductAsync(product);
            if (!success)
            {
                return Json(new { success = false, message = errorMessage ?? "Failed to update product" });
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

            // Kiểm tra status có hợp lệ không bằng cách sử dụng enum
            if (string.IsNullOrEmpty(status) || !Enum.TryParse<ProductStatus>(status, out _))
            {
                return Json(new { success = false, message = "Trạng thái không hợp lệ" });
            }

            product.Status = status;
            var (success, errorMessage) = await _productService.UpdateProductAsync(product);

            if (!success)
            {
                return Json(new { success = false, message = errorMessage ?? "Không thể cập nhật trạng thái sản phẩm" });
            }

            return Json(new { success = true });
        }

        // GET: Product/Favorites
        [Authorize]
        public async Task<IActionResult> Favorites(int page = 1)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            int pageSize = 12;

            var (items, totalCount, pageCount, currentPage, _) = await _productService.GetUserFavoritesPagedAsync(userId, page, pageSize);
            var categories = await _productService.GetAllCategoriesAsync();

            var productListDto = new ProductListDto
            {
                Products = items,
                Categories = categories,
                CurrentPage = currentPage,
                TotalPages = pageCount,
                Title = "Sản phẩm yêu thích"
            };

            return View(productListDto);
        }

        // POST: Product/AddFavorite
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddFavorite(int productId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            var (success, errorMessage) = await _productService.AddFavoriteAsync(userId, productId);

            if (!success)
            {
                return Json(new { success = false, message = errorMessage ?? "Không thể thêm vào danh sách yêu thích" });
            }

            return Json(new { success = true });
        }

        // POST: Product/RemoveFavorite
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RemoveFavorite(int productId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            var (success, errorMessage) = await _productService.RemoveFavoriteAsync(userId, productId);

            if (!success)
            {
                return Json(new { success = false, message = errorMessage ?? "Không thể xóa khỏi danh sách yêu thích" });
            }

            return Json(new { success = true });
        }

        // GET: Product/IsFavorite
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> IsFavorite(int productId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var isFavorite = await _productService.IsFavoriteAsync(userId, productId);

            return Json(new { isFavorite });
        }
    }
}
