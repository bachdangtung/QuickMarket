using System.Collections.Generic;

namespace BussinessLogic.DTOs.Users
{
    public class UserManagementDto
    {
        public List<UserListDto> Users { get; set; } = new List<UserListDto>();
        public string? SearchQuery { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
    }
}
