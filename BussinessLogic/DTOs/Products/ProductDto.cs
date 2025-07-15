using System;
using System.Collections.Generic;

namespace BussinessLogic.DTOs.Products
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public int? UserId { get; set; }
        public int? CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public DateTime DatePosted { get; set; }
        public string Status { get; set; }

        // Navigation properties (simplified for DTO)
        public string CategoryName { get; set; }
        public string SellerName { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
        public List<ProductReviewDto> Reviews { get; set; } = new List<ProductReviewDto>();
    }
}
