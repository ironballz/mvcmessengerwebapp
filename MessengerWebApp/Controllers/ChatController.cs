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
        private static List<WebSocketClient> clients = new List<WebSocketClient>();
        private Guid? clientIdentityHolder;

        MessengerWebAppDatabaseEntities context = new MessengerWebAppDatabaseEntities();

        // GET: Chat
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

        [HttpGet]
        public JsonResult GetMessages(Guid clientId)
        {
            User user = context.User.SingleOrDefault(x => x.UserId == clientId);
            List<ChatPostedMessage> messages = new List<ChatPostedMessage>();
            if (user != null)
            {
                List<Message> offlineMessages = context.Message.Where(x => x.RecordDate > user.LastActivityDate && !x.IsDeleted &&
                (!x.UserReceiverId.HasValue || (x.UserReceiverId.HasValue && (x.UserSenderId == clientId || x.UserReceiverId == clientId)))).OrderBy(x=>x.RecordDate).ToList();

                foreach (var message in offlineMessages)
                {
                    messages.Add(ConvertToPostedMessage(message));
                }
            }

            return Json(messages, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetPrivateMessagesHistory(Guid clientId, Guid receiverId)
        {
            List<ChatPostedMessage> messages = new List<ChatPostedMessage>();
            List<Message> privateMessages = context.Message.Where(x =>!x.IsDeleted && ((x.UserSenderId == clientId && x.UserReceiverId == receiverId) || (x.UserSenderId == receiverId && x.UserReceiverId == clientId)))
                .OrderByDescending(x => x.RecordDate).Take(1000).OrderBy(x=>x.RecordDate).ToList();

            foreach (var message in privateMessages)
            {
                messages.Add(ConvertToPostedMessage(message));
            }

            return Json(messages, JsonRequestBehavior.AllowGet);
        }

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

        private async Task ProcessWebSocketMessage(AspNetWebSocketContext webSocketContext)
        {
            var clientSocket = webSocketContext.WebSocket;
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

            while (true)
            {
                ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
                WebSocketReceiveResult result = await clientSocket.ReceiveAsync(buffer, CancellationToken.None);
                if (clientSocket.State == WebSocketState.Open)
                {
                    var messageJson = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                    var socketMessage = JsonConvert.DeserializeObject<ChatWebSocketMessage>(messageJson);

                    User sender = context.User.SingleOrDefault(x => x.UserId == socketMessage.ClientId);
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
                            if (postedMessage.Id.HasValue)
                            {
                                if (!postedMessage.IsDeleted)
                                {
                                    message = context.Message.SingleOrDefault(x => x.MessageId == postedMessage.Id);
                                    message.Content = postedMessage.Content;
                                    message.ModifiedDate = DateTime.Now;
                                }
                                else {
                                    message = context.Message.SingleOrDefault(x => x.MessageId == postedMessage.Id);
                                    message.ModifiedDate = DateTime.Now;
                                    message.IsDeleted = true;
                                }
                            }
                            else
                            {
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

                    buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(socketMessage)));

                    for (int i = 0; i < clients.Count; i++)
                    {
                        try
                        {
                            if (socketMessage.PostedMessage != null)
                            {
                                if (socketMessage.PostedMessage.ReceiverId.HasValue)
                                {
                                    if (clients[i].Identity == socketMessage.ClientId || clients[i].Identity == socketMessage.PostedMessage.ReceiverId)
                                    {
                                        await clients[i].WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                                    }
                                }
                                else {
                                    await clients[i].WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                            }
                            else {
                                await clients[i].WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                        }
                        catch
                        {
                            clients.Remove(clients[i]);
                            i--;
                        }
                    }
                }
            }
        }

        [NonAction]
        public ChatPostedMessage ConvertToPostedMessage(Message message) {
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