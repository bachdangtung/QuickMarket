using System;

namespace BussinessLogic.DTOs.Products
{
    public class ProductReviewDto
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime ReviewDate { get; set; }
        public int? ThreadId { get; set; }
        public ProductReviewDto[] Replies { get; set; }
    }
}
