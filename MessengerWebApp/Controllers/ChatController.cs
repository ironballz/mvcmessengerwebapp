using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MessengerWebApp.Models;
using MessengerWebApp.ViewModels;

namespace MessengerWebApp.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        MessengerWebAppDatabaseEntities context = new MessengerWebAppDatabaseEntities();

        // GET: Chat
        public ActionResult Index()
        {
            if (Session["UserId"] != null)
            {
                var userId = (Guid)Session["UserId"];
                //ChatDataViewModel chatData = new ChatDataViewModel() { };
                //chatData.CurrentUserID = (Guid)Session["UserId"];
                //chatData.Users = context.User.Where(x => x.UserID != chatData.CurrentUserID)
                //    .Select(x => new ChatUserViewModel()
                //    {
                //        UserID = x.UserID,
                //        Login = x.Login,
                //        IsOnline = x.IsOnline
                //    }).OrderBy(x => x.Login).ToList();

                //chatData.Messages = new List<ChatMessageViewModel>();
                //chatData.Messages.Add(new ChatMessageViewModel()
                //{
                //    MessageID = Guid.NewGuid(),
                //    UserSender = "ironballz",
                //    Content = "First test message. Its alive!",
                //    RecordDate = DateTime.Now
                //});

                //chatData.Messages.Add(new ChatMessageViewModel()
                //{
                //    MessageID = Guid.NewGuid(),
                //    UserSender = "alicecrowford",
                //    Content = "Secont test message. Its still alive!",
                //    RecordDate = DateTime.Now
                //});

                //chatData.Messages.Add(new ChatMessageViewModel()
                //{
                //    MessageID = Guid.NewGuid(),
                //    UserSender = "ironballz",
                //    Content = "Third test message.",
                //    RecordDate = DateTime.Now
                //});

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
            messages.Add(new ChatMessageViewModel()
            {
                Id = Guid.NewGuid(),
                SenderId = Guid.Parse("5a1044b2-eef1-4a38-9328-7cfd3a9f2a8e"),
                SenderName = "ironballz",
                Content = "First test message. Its alive!",
                RecordDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            });

            messages.Add(new ChatMessageViewModel()
            {
                Id = Guid.NewGuid(),
                SenderId = Guid.Parse("b0b8c503-0db6-4990-9e28-52520f47cc3f"),
                SenderName = "alicecrowford",
                Content = "Secont test message. Its still alive!",
                RecordDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            });

            messages.Add(new ChatMessageViewModel()
            {
                Id = Guid.NewGuid(),
                SenderId = Guid.Parse("5a1044b2-eef1-4a38-9328-7cfd3a9f2a8e"),
                SenderName = "ironballz",
                Content = "Third test message.",
                RecordDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            });

            return Json(messages, JsonRequestBehavior.AllowGet);
        }
    }
}