using System;

namespace BussinessLogic.DTOs.Products
{
    public class ProductImageDto
    {
        public int ImageId { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
