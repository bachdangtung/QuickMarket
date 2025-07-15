using BussinessLogic.DTOs;
using BussinessLogic.DTOs.Categories;
using BussinessLogic.DTOs.Products;
using BussinessLogic.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickMarket.Models;
using Services.Interfaces;
using System.Security.Claims;
using AutoMapper;
using QuickMarket.Helpers;

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
        public async Task<IActionResult> Index(string searchQuery = null, int? categoryId = null, string sortOrder = null, int page = 1)
        {
            var pageSize = 12; // Số sản phẩm trên mỗi trang
            
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
            
            // Sử dụng extension method để map PagedResult<ProductDto> sang PagedResult<ProductViewModel>
            var pagedViewModelResult = pagedResult.ToMappedPagedResult<ProductDto, ProductViewModel>(_mapper);

            var categories = await _productService.GetAllCategoriesAsync();

            var viewModel = new ProductListViewModel
            {
                Products = pagedViewModelResult.Items,
                Categories = categories,  
                SearchQuery = searchQuery,
                CategoryFilter = categoryId,
                SortOrder = sortOrder,
                CurrentPage = pagedViewModelResult.CurrentPage,
                TotalPages = pagedViewModelResult.PageCount
            };

            return View(viewModel);
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var productDto = await _productService.GetProductByIdAsync(id);
            if (productDto == null)
            {
                return NotFound();
            }

            // Sử dụng AutoMapper để chuyển đổi từ ProductReviewDto sang ProductReviewViewModel
            var allReviewsVM = _mapper.Map<List<ProductReviewViewModel>>(productDto.Reviews);

            // Tách riêng reviews gốc (không có ThreadId) và replies (có ThreadId)
            var mainReviews = allReviewsVM.Where(r => r.ThreadId == null).ToList();
            var replies = allReviewsVM.Where(r => r.ThreadId != null).ToList();

            // Gán replies vào review gốc tương ứng
            foreach (var reply in replies)
            {
                var parentReview = allReviewsVM.FirstOrDefault(r => r.ReviewId == reply.ThreadId);
                if (parentReview != null)
                {
                    if (parentReview.Replies == null)
                        parentReview.Replies = new List<ProductReviewViewModel>();
                    
                    parentReview.Replies.Add(reply);
                }
            }

            // Sử dụng AutoMapper để chuyển đổi từ ProductDto sang ProductViewModel
            var viewModel = _mapper.Map<ProductViewModel>(productDto);
            viewModel.Reviews = mainReviews; // Chỉ lấy các reviews gốc (các replies đã được gắn vào reviews gốc)

            return View(viewModel);
        }

        // GET: Product/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _productService.GetAllCategoriesAsync();
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(ProductViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Map từ ViewModel sang Entity
                var product = _mapper.Map<Product>(viewModel);
                product.DatePosted = DateTime.Now;
                product.Status = "Active";
                product.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Convert the Product to ProductDto for the service or ensure the service accepts Product type
                var productDto = _mapper.Map<ProductDto>(product);
                var result = await _productService.CreateProductAsync(productDto);
                
                if (result)
                {
                    // Xử lý tải lên hình ảnh
                    if (viewModel.ImageFiles != null && viewModel.ImageFiles.Count > 0)
                    {
                        await SaveProductImages(product.ProductId, viewModel.ImageFiles);
                    }

                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.Categories = await _productService.GetAllCategoriesAsync();
            return View(viewModel);
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
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (productDto.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var viewModel = _mapper.Map<ProductViewModel>(productDto);

            ViewBag.Categories = await _productService.GetAllCategoriesAsync();
            return View(viewModel);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, ProductViewModel viewModel)
        {
            if (id != viewModel.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var productDto = await _productService.GetProductByIdAsync(id);
                if (productDto == null)
                {
                    return NotFound();
                }

                // Kiểm tra xem người dùng hiện tại có phải là chủ sở hữu hoặc admin không
                var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (productDto.UserId != currentUserId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                // Cập nhật thông tin sản phẩm từ ViewModel vào DTO
                var updatedProductDto = _mapper.Map<ProductDto>(viewModel);
                updatedProductDto.ProductId = id;
                
                // Giữ nguyên thông tin không thay đổi
                updatedProductDto.UserId = productDto.UserId;
                updatedProductDto.DatePosted = productDto.DatePosted;
                updatedProductDto.ImageUrls = productDto.ImageUrls;

                var result = await _productService.UpdateProductAsync(updatedProductDto);

                if (result)
                {
                    // Xử lý tải lên hình ảnh mới nếu có
                    if (viewModel.ImageFiles != null && viewModel.ImageFiles.Count > 0)
                    {
                        await SaveProductImages(updatedProductDto.ProductId, viewModel.ImageFiles);
                    }

                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.Categories = await _productService.GetAllCategoriesAsync();
            return View(viewModel);
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
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (productDto.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var viewModel = _mapper.Map<ProductViewModel>(productDto);

            return View(viewModel);
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
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (product.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            await _productService.DeleteProductAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Product/ManageProducts
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ManageProducts(string searchQuery = null, int? categoryId = null, string sortOrder = null, int page = 1)
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
            
            // Sử dụng extension method để map PagedResult<ProductDto> sang PagedResult<ProductViewModel>
            var pagedViewModelResult = pagedResult.ToMappedPagedResult<ProductDto, ProductViewModel>(_mapper);

            var categories = await _productService.GetAllCategoriesAsync();

            var viewModel = new ProductListViewModel
            {
                Products = pagedViewModelResult.Items,
                Categories = categories,
                SearchQuery = searchQuery,
                CategoryFilter = categoryId,
                SortOrder = sortOrder,
                CurrentPage = pagedViewModelResult.CurrentPage,
                TotalPages = pagedViewModelResult.PageCount
            };

            return View(viewModel);
        }

        // GET: Product/MyProducts
        [Authorize]
        public async Task<IActionResult> MyProducts(int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var pageSize = 10; // Số sản phẩm trên mỗi trang
            
            // Sử dụng phiên bản có phân trang
            var pagedResult = await _productService.GetProductsByUserPagedAsync(userId, page, pageSize);
            
            // Sử dụng extension method để map PagedResult<ProductDto> sang PagedResult<ProductViewModel>
            var pagedViewModelResult = pagedResult.ToMappedPagedResult<ProductDto, ProductViewModel>(_mapper);
            
            // Tạo một viewModel để bao gồm thông tin phân trang
            var viewModel = new ProductListViewModel
            {
                Products = pagedViewModelResult.Items,
                CurrentPage = pagedViewModelResult.CurrentPage,
                TotalPages = pagedViewModelResult.PageCount
            };
            
            return View(viewModel);
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

            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
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
            if (product.UserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)))
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
