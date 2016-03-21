using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Web;

namespace MessengerWebApp.Models
{
    public enum ChatWebSocketMessageType
    {
        Join,
        Message,
        Leave
    }

    public class WebSocketClient
    {
        public Guid Identity { get; set; }
        public WebSocket WebSocket { get; set; }
    }

    public class ChatWebSocketMessage
    {
        public ChatWebSocketMessageType Type { get; set; }
        public Guid ClientId { get; set; }
        public ChatUserInfo UserInfo { get; set; }
        public ChatPostedMessage PostedMessage { get; set; }
    }

    public class ChatUserInfo
    {
        public Guid Id { get; set; }
        public string Login { get; set; }
        public bool IsOnline { get; set; }
    }

    public class ChatPostedMessage
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