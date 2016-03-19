using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.WebSockets;

namespace MessengerWebApp.Models
{
    public class WebSocketClient
    {
        public Guid Identity { get; set; }
        public WebSocket WebSocket { get; set; }
    }
}