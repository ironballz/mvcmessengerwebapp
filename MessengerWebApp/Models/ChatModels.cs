using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MessengerWebApp.Models
{
    public enum ChatWebSocketMessageType
    {
        Join,
        Message,
        Leave
    }

    public class ChatWebSocketMessage
    {
        public ChatWebSocketMessageType Type { get; set; }
        public Guid ClientId { get; set; }
        public ChatUser User { get; set; }
        public ChatMessage PostedMessage { get; set; }
    }

    public class ChatUser
    {
        public Guid Id { get; set; }
        public string Login { get; set; }
        public bool IsOnline { get; set; }
    }

    public class ChatMessage
    {
        public Guid? Id { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; }
        public Guid? ReceiverId { get; set; }
        public string Content { get; set; }
        public string RecordDate { get; set; }
        public string ModifiedDate { get; set; }
        public bool IsDeleted { get; set; }
    }
}