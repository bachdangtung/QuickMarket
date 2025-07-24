using BussinessLogic.DTOs.Categories;
using System.Collections.Generic;

namespace BussinessLogic.DTOs.Products
{
    public class ProductListDto
    {
        public List<ProductDto> Products { get; set; } = new List<ProductDto>();
        public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
        public string? SearchQuery { get; set; }
        public int? CategoryFilter { get; set; }
        public string? SortOrder { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public string? Title { get; set; }
    }
}
