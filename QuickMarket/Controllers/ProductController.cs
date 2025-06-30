using BussinessLogic.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickMarket.Models;
using Services.Interfaces;
using System.Security.Claims;

namespace QuickMarket.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        // GET: Product
        public async Task<IActionResult> Index(string searchQuery = null, int? categoryId = null, string sortOrder = null, int page = 1)
        {
            var pageSize = 12; // Số sản phẩm trên mỗi trang
            
            var allProducts = await _productService.GetAllProductsAsync();
            var filteredProducts = allProducts.AsQueryable();

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchQuery))
            {
                filteredProducts = filteredProducts.Where(p => 
                    p.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) || 
                    (p.Description != null && p.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)));
            }

            // Lọc theo danh mục
            if (categoryId.HasValue && categoryId > 0)
            {
                filteredProducts = filteredProducts.Where(p => p.CategoryId == categoryId);
            }

            // Sắp xếp
            filteredProducts = sortOrder switch
            {
                "price_asc" => filteredProducts.OrderBy(p => p.Price),
                "price_desc" => filteredProducts.OrderByDescending(p => p.Price),
                "newest" => filteredProducts.OrderByDescending(p => p.DatePosted),
                _ => filteredProducts.OrderByDescending(p => p.DatePosted), // Mặc định sắp xếp theo ngày đăng, mới nhất lên đầu
            };

            // Phân trang
            var totalItems = filteredProducts.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            var pagedProducts = filteredProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Chuyển đổi sang ViewModel
            var productViewModels = pagedProducts.Select(p => new ProductViewModel
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                DatePosted = p.DatePosted,
                Status = p.Status,
                CategoryId = p.CategoryId ?? 0,
                CategoryName = p.Category?.CategoryName,
                SellerName = p.User?.Username,
                UserId = p.UserId,
                ExistingImageUrls = p.ProductImages.Select(img => img.ImageUrl).ToList()
            }).ToList();

            var categories = await _productService.GetAllCategoriesAsync();

            var viewModel = new ProductListViewModel
            {
                Products = productViewModels,
                Categories = categories,
                SearchQuery = searchQuery,
                CategoryFilter = categoryId,
                SortOrder = sortOrder,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View(viewModel);
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Chuyển đổi tất cả reviews thành view models
            var allReviewsVM = product.ProductReviews.Select(r => new ProductReviewViewModel
            {
                ReviewId = r.ReviewId,
                ProductId = r.ProductId ?? 0,
                UserId = r.UserId ?? 0,
                UserName = r.User?.Username ?? "Unknown",
                Rating = r.Rating ?? 0,
                Comment = r.Comment,
                ReviewDate = r.ReviewDate,
                ThreadId = r.ThreadId
            }).ToList();

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

            var viewModel = new ProductViewModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                DatePosted = product.DatePosted,
                Status = product.Status,
                CategoryId = product.CategoryId ?? 0,
                CategoryName = product.Category?.CategoryName,
                SellerName = product.User?.Username,
                UserId = product.UserId,
                ExistingImageUrls = product.ProductImages.Select(img => img.ImageUrl).ToList(),
                Reviews = mainReviews // Chỉ lấy các reviews gốc (các replies đã được gắn vào reviews gốc)
            };

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
                var product = new Product
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    Price = viewModel.Price,
                    DatePosted = DateTime.Now,
                    Status = "Active",
                    CategoryId = viewModel.CategoryId,
                    UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
                };

                var result = await _productService.CreateProductAsync(product);
                
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

            var viewModel = new ProductViewModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Status = product.Status,
                CategoryId = product.CategoryId ?? 0,
                UserId = product.UserId,
                ExistingImageUrls = product.ProductImages.Select(img => img.ImageUrl).ToList()
            };

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

                // Cập nhật thông tin sản phẩm
                product.Name = viewModel.Name;
                product.Description = viewModel.Description;
                product.Price = viewModel.Price;
                product.Status = viewModel.Status;
                product.CategoryId = viewModel.CategoryId;

                var result = await _productService.UpdateProductAsync(product);

                if (result)
                {
                    // Xử lý tải lên hình ảnh mới nếu có
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

        // GET: Product/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int id)
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

            var viewModel = new ProductViewModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                DatePosted = product.DatePosted,
                Status = product.Status,
                CategoryId = product.CategoryId ?? 0,
                CategoryName = product.Category?.CategoryName,
                SellerName = product.User?.Username,
                ExistingImageUrls = product.ProductImages.Select(img => img.ImageUrl).ToList()
            };

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
            
            var allProducts = await _productService.GetAllProductsAsync();
            var filteredProducts = allProducts.AsQueryable();

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchQuery))
            {
                filteredProducts = filteredProducts.Where(p => 
                    p.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) || 
                    (p.Description != null && p.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)));
            }

            // Lọc theo danh mục
            if (categoryId.HasValue && categoryId > 0)
            {
                filteredProducts = filteredProducts.Where(p => p.CategoryId == categoryId);
            }

            // Sắp xếp
            filteredProducts = sortOrder switch
            {
                "name_asc" => filteredProducts.OrderBy(p => p.Name),
                "name_desc" => filteredProducts.OrderByDescending(p => p.Name),
                "price_asc" => filteredProducts.OrderBy(p => p.Price),
                "price_desc" => filteredProducts.OrderByDescending(p => p.Price),
                "newest" => filteredProducts.OrderByDescending(p => p.DatePosted),
                "oldest" => filteredProducts.OrderBy(p => p.DatePosted),
                _ => filteredProducts.OrderByDescending(p => p.DatePosted), // Mặc định sắp xếp theo ngày đăng, mới nhất lên đầu
            };

            // Phân trang
            var totalItems = filteredProducts.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            var pagedProducts = filteredProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Chuyển đổi sang ViewModel
            var productViewModels = pagedProducts.Select(p => new ProductViewModel
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                DatePosted = p.DatePosted,
                Status = p.Status,
                CategoryId = p.CategoryId ?? 0,
                CategoryName = p.Category?.CategoryName,
                SellerName = p.User?.Username,
                UserId = p.UserId,
                ExistingImageUrls = p.ProductImages.Select(img => img.ImageUrl).ToList()
            }).ToList();

            var categories = await _productService.GetAllCategoriesAsync();

            var viewModel = new ProductListViewModel
            {
                Products = productViewModels,
                Categories = categories,
                SearchQuery = searchQuery,
                CategoryFilter = categoryId,
                SortOrder = sortOrder,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View(viewModel);
        }

        // GET: Product/MyProducts
        [Authorize]
        public async Task<IActionResult> MyProducts()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var products = await _productService.GetProductsByUserAsync(userId);
            
            var productViewModels = products.Select(p => new ProductViewModel
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                DatePosted = p.DatePosted,
                Status = p.Status,
                CategoryId = p.CategoryId ?? 0,
                CategoryName = p.Category?.CategoryName,
                ExistingImageUrls = p.ProductImages.Select(img => img.ImageUrl).ToList()
            }).ToList();
            
            return View(productViewModels);
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
                if (product.ProductReviews.Any(r => r.UserId == currentUserId && r.ThreadId == null))
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

            // Thêm đánh giá vào sản phẩm
            product.ProductReviews.Add(review);
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
                        // Thêm hình ảnh vào sản phẩm
                        product.ProductImages.Add(new ProductImage
                        {
                            ProductId = productId,
                            ImageUrl = imageUrl,
                            DateAdded = DateTime.Now
                        });
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
            // First find which product has this image
            var allProducts = await _productService.GetAllProductsAsync();
            var product = allProducts.FirstOrDefault(p => p.ProductImages.Any(i => i.ImageId == imageId));
            
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found for this image" });
            }
            
            var imageToRemove = product.ProductImages.FirstOrDefault(i => i.ImageId == imageId);
            
            if (imageToRemove == null)
            {
                return Json(new { success = false, message = "Image not found" });
            }
            
            // Verify the user owns the product
            if (product.UserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)))
            {
                return Json(new { success = false, message = "You are not authorized to delete this image" });
            }
            
            // Delete from Cloudinary
            var cloudDeleteResult = await _productService.DeleteProductImageAsync(imageToRemove.ImageUrl);
            if (!cloudDeleteResult)
            {
                return Json(new { success = false, message = "Failed to delete image from cloud storage" });
            }
            
            // Remove from product's image collection
            product.ProductImages.Remove(imageToRemove);
            
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
