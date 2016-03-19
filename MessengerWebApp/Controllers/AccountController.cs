using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using MessengerWebApp.Models;
using MessengerWebApp.ViewModels;

namespace MessengerWebApp.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        MessengerWebAppDatabaseEntities context = new MessengerWebAppDatabaseEntities();

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel registrationData)
        {
            if (ModelState.IsValid)
            {
                var userExists = context.User.Any(x => x.Login == registrationData.Login);

                if (!userExists)
                {
                    User user = new User()
                    {
                        UserId = Guid.NewGuid(),
                        Login = registrationData.Login,
                        Password = registrationData.Password,
                        FirstName = registrationData.FirstName,
                        LastName = registrationData.LastName,
                        Email = registrationData.Email,
                        ActivityTimeout = 15
                    };

                    context.User.Add(user);
                    context.SaveChanges();
                    return RedirectToAction("SignIn");
                }
                else {
                    ModelState.AddModelError("", "User with the same login is already exist.");
                }
            }

            return View(registrationData);
        }

        //
        // GET: /Account/SignIn
        [AllowAnonymous]
        public ActionResult SignIn()
        {
            if (FormsAuthentication.CookiesSupported) {
                var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
                if (authCookie != null && Session["UserId"] != null) {
                    return RedirectToAction("Index", "Chat");
                }
            }
            return View();
        }

        //
        // POST: /Account/SignIn
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult SignIn(SignInViewModel credentials)
        {
            if (ModelState.IsValid)
            {
                User user = context.User.SingleOrDefault(x => x.Login == credentials.Login && x.Password == credentials.Password);
                if (user != null)
                {
                    user.IsOnline = true;
                    user.LastActivityDate = null;

                    FormsAuthentication.SetAuthCookie(credentials.Login, credentials.RememberMe);
                    Session["UserId"] = user.UserId;
                    Session["UserLogin"] = user.Login;
                    Session.Timeout = user.ActivityTimeout;

                    return RedirectToAction("Index", "Chat");
                }
                else
                {
                    ModelState.AddModelError("", "You have entered incorrect login or password.");
                }
            }

            return View(credentials);
        }

        //
        // GET: /Account/SignOut
        public ActionResult SignOut()
        {
            var userId = (Guid)Session["UserId"];
            var user = context.User.SingleOrDefault(x => x.UserId == userId);
            if (user != null) {
                user.IsOnline = false;
                user.LastActivityDate = DateTime.Now;
            }
            FormsAuthentication.SignOut();
            Session.Abandon();
            return RedirectToAction("SignIn");
        }
    }
}