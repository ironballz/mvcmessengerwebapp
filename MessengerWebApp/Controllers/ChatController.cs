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
        public JsonResult GetUsers(Guid clientId)
        {
            var users = context.User.Where(x => x.UserId != clientId)
                .Select(x => new ChatUser()
                {
                    Id = x.UserId,
                    Login = x.Login,
                    IsOnline = x.IsOnline
                }).OrderBy(x => x.Login).ToList();
            return Json(users, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetMessages(Guid clientId)
        {
            var messages = new List<ChatMessage>();

            return Json(messages, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public void WebSocketHandler(Guid clientId) {
            var httpContext = System.Web.HttpContext.Current;
            if (httpContext.IsWebSocketRequest)
            {
                clientIdentityHolder = clientId;
                httpContext.AcceptWebSocketRequest(ProcessWebSocketMessage);
            }
        }

        private async Task ProcessWebSocketMessage(AspNetWebSocketContext webSocketContext) {
            var clientSocket = webSocketContext.WebSocket;

            try
            {
                clients.Add(new WebSocketClient()
                {
                    Identity = clientIdentityHolder.Value,
                    WebSocket = clientSocket
                });
            }
            finally {
                clientIdentityHolder = null;
            }

            while (true) {
                ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
                WebSocketReceiveResult result = await clientSocket.ReceiveAsync(buffer, CancellationToken.None);
                if (clientSocket.State == WebSocketState.Open) {
                    var messageJson = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                    var socketMessage = JsonConvert.DeserializeObject<ChatWebSocketMessage>(messageJson);

                    User sender = context.User.SingleOrDefault(x => x.UserId == socketMessage.ClientId);
                    switch (socketMessage.Type) {
                        case ChatWebSocketMessageType.Join:
                            socketMessage.User.Login = sender.Login;
                            break;
                        case ChatWebSocketMessageType.Leave:
                            socketMessage.User.Login = sender.Login;
                            break;
                        case ChatWebSocketMessageType.Message:
                            var postedMessage = socketMessage.PostedMessage;
                            Message message = new Message()
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
                            context.SaveChanges();

                            socketMessage.PostedMessage.Id = message.MessageId;
                            socketMessage.PostedMessage.SenderId = message.UserSenderId;
                            socketMessage.PostedMessage.SenderName = sender.Login;
                            socketMessage.PostedMessage.RecordDate = message.RecordDate.ToString("dd.MM.yyyy HH:mm:ss");
                            socketMessage.PostedMessage.ModifiedDate = message.ModifiedDate.HasValue ? message.ModifiedDate.Value.ToString("dd.MM.yyyy HH:mm:ss") : null;
                            socketMessage.PostedMessage.IsDeleted = message.IsDeleted;                            
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
                        catch {
                            clients.Remove(clients[i]);
                            i--;
                        }                        
                    }
                }
            }
        }
    }
}