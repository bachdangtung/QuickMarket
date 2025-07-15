using System;

namespace BussinessLogic.DTOs.Users
{
    public class ExternalLoginDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Provider { get; set; }
        public string ProviderKey { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
