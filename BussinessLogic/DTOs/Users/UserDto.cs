using System;

namespace BussinessLogic.DTOs.Users
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? LastLogin { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string Status { get; set; }
    }
}
