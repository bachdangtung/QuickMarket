using System;

namespace BussinessLogic.DTOs.Favorites
{
    public class FavoriteDto
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
