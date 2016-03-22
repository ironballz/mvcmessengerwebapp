using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.WebSockets;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using MessengerWebApp.Models;
using MessengerWebApp.ViewModels;

namespace MessengerWebApp.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        // Web socket connected clients list.
        private static List<WebSocketClient> clients = new List<WebSocketClient>();
        // Client identity temporary variable.
        private Guid? clientIdentityHolder;

        MessengerWebAppDatabaseEntities context = new MessengerWebAppDatabaseEntities();

        //
        // GET: /Chat
        public ActionResult Index()
        {
            if (Session["UserId"] != null)
            {
                var userId = (Guid)Session["UserId"];
                return View(userId);
            }
            else {
                return RedirectToAction("SignIn", "Account");
            }
        }

        //
        // GET: /Chat/GetUsersInfo
        // Returns chat users information.
        [HttpGet]
        public JsonResult GetUsersInfo(Guid clientId)
        {
            List<ChatUserInfo> userInfos = context.User.Where(x => x.UserId != clientId)
                .Select(x => new ChatUserInfo()
                {
                    Id = x.UserId,
                    Login = x.Login,
                    IsOnline = x.IsOnline
                }).OrderBy(x => x.Login).ToList();
            return Json(userInfos, JsonRequestBehavior.AllowGet);
        }

        //
        // GET: /Chat/GetMessages
        // Returns all client messages received during his offline time.
        [HttpGet]
        public JsonResult GetMessages(Guid clientId)
        {
            User user = context.User.SingleOrDefault(x => x.UserId == clientId);
            List<ChatPostedMessage> messages = new List<ChatPostedMessage>();
            if (user != null)
            {
                List<Message> offlineMessages = context.Message.Where(x => x.RecordDate > user.LastActivityDate && !x.IsDeleted &&
                (!x.UserReceiverId.HasValue || (x.UserReceiverId.HasValue && (x.UserSenderId == clientId || x.UserReceiverId == clientId)))).OrderBy(x => x.RecordDate).ToList();

                foreach (var message in offlineMessages)
                {
                    messages.Add(ConvertToPostedMessage(message));
                }
            }

            return Json(messages, JsonRequestBehavior.AllowGet);
        }

        //
        // GET: /Chat/GetPrivateMessagesHistory
        // Returns private messaging history between two users.
        [HttpGet]
        public JsonResult GetPrivateMessagesHistory(Guid clientId, Guid receiverId)
        {
            List<ChatPostedMessage> messages = new List<ChatPostedMessage>();
            List<Message> privateMessages = context.Message.Where(x => !x.IsDeleted && ((x.UserSenderId == clientId && x.UserReceiverId == receiverId) || (x.UserSenderId == receiverId && x.UserReceiverId == clientId)))
                .OrderByDescending(x => x.RecordDate).Take(1000).OrderBy(x => x.RecordDate).ToList();

            foreach (var message in privateMessages)
            {
                messages.Add(ConvertToPostedMessage(message));
            }

            return Json(messages, JsonRequestBehavior.AllowGet);
        }

        //
        // GET: /Chat/ProlongSessionLifetime
        // This action was created to prolong session lifetime.
        [HttpGet]
        public JsonResult ProlongSessionLifetime()
        {
            if (Session.Keys.Count == 0)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            else {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
        }

        //
        // GET: /Chat/WebSocketHandler
        // Web socket connection action.
        [HttpGet]
        public void WebSocketHandler(Guid clientId)
        {
            var httpContext = System.Web.HttpContext.Current;
            if (httpContext.IsWebSocketRequest)
            {
                clientIdentityHolder = clientId;
                httpContext.AcceptWebSocketRequest(ProcessWebSocketMessage);
            }
        }

        // Web socket processing method.
        private async Task ProcessWebSocketMessage(AspNetWebSocketContext webSocketContext)
        {
            // Get client from web socket context.
            var clientSocket = webSocketContext.WebSocket;

            // Add client to clients list.
            try
            {
                clients.Add(new WebSocketClient()
                {
                    Identity = clientIdentityHolder.Value,
                    WebSocket = clientSocket
                });
            }
            finally
            {
                clientIdentityHolder = null;
            }

            // Listen to web socket messages.
            while (true)
            {
                ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
                // Wait for web socket message.
                WebSocketReceiveResult result = await clientSocket.ReceiveAsync(buffer, CancellationToken.None);
                // Get message JSON string from web socket message buffer.
                var messageJson = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                // Deserialize web socket message.
                var socketMessage = JsonConvert.DeserializeObject<ChatWebSocketMessage>(messageJson);

                User sender = context.User.SingleOrDefault(x => x.UserId == socketMessage.ClientId);
                // Process message according to its type.
                switch (socketMessage.Type)
                {
                    case ChatWebSocketMessageType.Join:
                        socketMessage.UserInfo.Login = sender.Login;
                        break;
                    case ChatWebSocketMessageType.Leave:
                        socketMessage.UserInfo.Login = sender.Login;
                        break;
                    case ChatWebSocketMessageType.Message:
                        var postedMessage = socketMessage.PostedMessage;

                        Message message;
                        // Check if it is existing message.
                        if (postedMessage.Id.HasValue)
                        {
                            if (!postedMessage.IsDeleted)
                            {
                                // Update existing message.
                                message = context.Message.SingleOrDefault(x => x.MessageId == postedMessage.Id);
                                message.Content = postedMessage.Content;
                                message.ModifiedDate = DateTime.Now;
                            }
                            else {
                                // Mark existing message as "deleted".
                                message = context.Message.SingleOrDefault(x => x.MessageId == postedMessage.Id);
                                message.ModifiedDate = DateTime.Now;
                                message.IsDeleted = true;
                            }
                        }
                        else
                        {
                            // Add new message to database.
                            message = new Message()
                            {
                                MessageId = Guid.NewGuid(),
                                UserSenderId = socketMessage.ClientId,
                                Content = postedMessage.Content,
                                UserReceiverId = postedMessage.ReceiverId,
                                RecordDate = DateTime.Now,
                                ModifiedDate = null,
                                IsDeleted = false
                            };

                            context.Message.Add(message);
                        }

                        context.SaveChanges();

                        socketMessage.PostedMessage = ConvertToPostedMessage(message);
                        break;
                }

                // Set serialized response web socket message data to buffer.
                buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(socketMessage)));

                // Send message to the clients.
                for (int i = 0; i < clients.Count; i++)
                {
                    try
                    {
                        if (clients[i].WebSocket.State == WebSocketState.Open)
                        {
                            // Check if web socket message contains posted message.
                            if (socketMessage.PostedMessage != null)
                            {
                                // Check if posted message is private.
                                if (socketMessage.PostedMessage.ReceiverId.HasValue)
                                {
                                    if (clients[i].Identity == socketMessage.ClientId || clients[i].Identity == socketMessage.PostedMessage.ReceiverId)
                                    {
                                        // Send posted message to sender and receiver clients.
                                        await clients[i].WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                                    }
                                }
                                else {
                                    // Send posted message to all clients.
                                    await clients[i].WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                            }
                            else {
                                // Send user status change message to all clients.
                                await clients[i].WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                        }
                    }
                    catch
                    {
                        // Remove client from list if it is no longer exists;
                        clients.Remove(clients[i]);
                        // Decrement loop counter.
                        i--;
                    }
                }
            }
        }

        // Returns posted message object converted from message.
        [NonAction]
        public ChatPostedMessage ConvertToPostedMessage(Message message)
        {
            User sender = context.User.SingleOrDefault(x => x.UserId == message.UserSenderId);
            ChatPostedMessage postedMessage = new ChatPostedMessage()
            {
                Id = message.MessageId,
                SenderId = message.UserSenderId,
                SenderName = message.User1.Login,
                Content = message.Content,
                ReceiverId = message.UserReceiverId,
                RecordDate = message.RecordDate.ToString("dd.MM.yyyy HH:mm:ss"),
                ModifiedDate = message.ModifiedDate.HasValue ? message.ModifiedDate.Value.ToString("dd.MM.yyyy HH:mm:ss") : null,
                IsDeleted = message.IsDeleted
            };

            return postedMessage;
        }
    }
}