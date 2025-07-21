using System;
using System.Collections.Generic;

namespace BussinessLogic.DTOs.Messages
{
    public class MessageDto
    {
        public int MessId { get; set; }
        public int FromUserId { get; set; }
        public string FromUsername { get; set; } = string.Empty;
        public int ToUserId { get; set; }
        public string ToUsername { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime SentTime { get; set; }
    }
    
    public class ChatHistoryDto
    {
        public int OtherUserId { get; set; }
        public string OtherUsername { get; set; } = string.Empty;
        public List<MessageDto> Messages { get; set; } = new List<MessageDto>();
        public ProductInfoDto? ProductInfo { get; set; }
    }
}
