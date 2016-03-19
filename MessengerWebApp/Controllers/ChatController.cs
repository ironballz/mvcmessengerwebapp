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
                .Select(x => new ChatUserViewModel()
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
            var messages = new List<ChatMessageViewModel>();
            //messages.Add(new ChatMessageViewModel()
            //{
            //    Id = Guid.NewGuid(),
            //    SenderId = Guid.Parse("5a1044b2-eef1-4a38-9328-7cfd3a9f2a8e"),
            //    SenderName = "ironballz",
            //    Content = "First test message. Its alive!",
            //    RecordDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
            //});

            //messages.Add(new ChatMessageViewModel()
            //{
            //    Id = Guid.NewGuid(),
            //    SenderId = Guid.Parse("b0b8c503-0db6-4990-9e28-52520f47cc3f"),
            //    SenderName = "alicecrowford",
            //    Content = "Secont test message. Its still alive!",
            //    RecordDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
            //});

            //messages.Add(new ChatMessageViewModel()
            //{
            //    Id = Guid.NewGuid(),
            //    SenderId = Guid.Parse("5a1044b2-eef1-4a38-9328-7cfd3a9f2a8e"),
            //    SenderName = "ironballz",
            //    Content = "Third test message.",
            //    RecordDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
            //});

            return Json(messages, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public void MessageHandler(Guid clientId) {
            var httpContext = System.Web.HttpContext.Current;
            if (httpContext.IsWebSocketRequest)
            {
                clientIdentityHolder = clientId;
                httpContext.AcceptWebSocketRequest(ProcessMessage);
            }
        }

        private async Task ProcessMessage(AspNetWebSocketContext webSocketContext) {
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
                    var receivedMessage = JsonConvert.DeserializeObject<Message>(messageJson);
                    receivedMessage.MessageId = Guid.NewGuid();
                    receivedMessage.RecordDate = DateTime.Now;

                    context.Message.Add(receivedMessage);
                    context.SaveChanges();

                    var sender = context.User.SingleOrDefault(x => x.UserId == receivedMessage.UserSenderId);
                    var responseMessage = new ChatMessageViewModel()
                    {
                        Id = receivedMessage.MessageId,
                        SenderId = receivedMessage.UserSenderId,
                        SenderName = sender.Login,
                        Content = receivedMessage.Content,
                        RecordDate = receivedMessage.RecordDate.ToString("dd.MM.yyyy HH:mm:ss"),
                        ModifiedDate = receivedMessage.ModifiedDate.HasValue ? receivedMessage.ModifiedDate.Value.ToString("dd.MM.yyyy HH:mm:ss") : null,
                        IsDeleted = receivedMessage.IsDeleted
                    };
                    buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseMessage)));

                    for (int i = 0; i < clients.Count; i++)
                    {
                        try
                        {
                            await clients[i].WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
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