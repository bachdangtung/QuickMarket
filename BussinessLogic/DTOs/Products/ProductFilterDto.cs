using System;
using System.Collections.Generic;

namespace BussinessLogic.DTOs.Products
{
    public class ProductFilterDto
    {
        public string? SearchQuery { get; set; }
        public int? CategoryId { get; set; }
        public string? SortOrder { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
