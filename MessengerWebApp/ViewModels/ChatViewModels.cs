using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MessengerWebApp.Models
{
    public class ChatUserViewModel
    {
        public Guid Id { get; set; }
        public string Login { get; set; }
        public bool IsOnline { get; set; }
    }

    public class ChatMessageViewModel
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public string RecordDate { get; set; }
        public string ModifiedDate { get; set; }
        public bool IsDeleted { get; set; }
    }
}