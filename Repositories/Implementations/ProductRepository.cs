using BussinessLogic.DTOs;
using BussinessLogic.DTOs.Products;
using BussinessLogic.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using System.Linq;

namespace Repositories.Implementations
{
    public class ProductRepository : IProductRepository
    {
        private readonly QuickMarketContext _context;

        public ProductRepository(QuickMarketContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.ProductImages)
                .ToListAsync();
        }

        public async Task<PagedResult<Product>> GetFilteredProductsAsync(ProductFilterDto filter)
        {
            // Bắt đầu với IQueryable để tận dụng lợi ích của truy vấn lazily
            IQueryable<Product> query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.ProductImages);

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(filter.SearchQuery))
            {
                query = query.Where(p => 
                    p.Name.Contains(filter.SearchQuery) || 
                    (p.Description != null && p.Description.Contains(filter.SearchQuery)));
            }

            // Lọc theo danh mục
            if (filter.CategoryId.HasValue && filter.CategoryId > 0)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId);
            }

            // Đếm tổng số kết quả trước khi phân trang
            var totalCount = await query.CountAsync();

            // Sắp xếp
            query = filter.SortOrder switch
            {
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "newest" => query.OrderByDescending(p => p.DatePosted),
                "oldest" => query.OrderBy(p => p.DatePosted),
                _ => query.OrderByDescending(p => p.DatePosted), // Default: newest first
            };

            // Thực hiện phân trang
            int pageSize = filter.PageSize;
            int skipAmount = (filter.Page - 1) * pageSize;

            // Thực thi truy vấn với phân trang
            var pagedData = await query
                .Skip(skipAmount)
                .Take(pageSize)
                .ToListAsync();

            // Tính toán số trang
            int pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Trả về kết quả đã phân trang
            return new PagedResult<Product>
            {
                Items = pagedData,
                TotalCount = totalCount,
                PageCount = pageCount,
                CurrentPage = filter.Page,
                PageSize = pageSize
            };
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductReviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.ProductImages)
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();
        }

        public async Task<List<Product>> GetProductsByUserAsync(int userId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }

        public async Task<PagedResult<Product>> GetProductsByUserPagedAsync(int userId, int page, int pageSize)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.DatePosted);

            // Đếm tổng số kết quả
            var totalCount = await query.CountAsync();

            // Thực hiện phân trang
            var skipAmount = (page - 1) * pageSize;
            var pagedData = await query
                .Skip(skipAmount)
                .Take(pageSize)
                .ToListAsync();

            // Tính toán số trang
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Trả về kết quả đã phân trang
            return new PagedResult<Product>
            {
                Items = pagedData,
                TotalCount = totalCount,
                PageCount = pageCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<bool> CreateProductAsync(Product product)
        {
            try
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                _context.Entry(product).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return false;

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ProductCategory>> GetAllCategoriesAsync()
        {
            return await _context.ProductCategories.ToListAsync();
        }

        public async Task<ProductCategory?> GetCategoryByIdAsync(int categoryId)
        {
            return await _context.ProductCategories.FindAsync(categoryId);
        }
        
        public async Task<ProductImage?> GetProductImageByIdAsync(int imageId)
        {
            return await _context.ProductImages
                .Include(pi => pi.Product)
                .FirstOrDefaultAsync(pi => pi.ImageId == imageId);
        }
    }
}
