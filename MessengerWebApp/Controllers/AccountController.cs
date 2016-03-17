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
        public ActionResult Register(RegisterViewModel registrationData)
        {
            if (ModelState.IsValid)
            {
                var userExists = context.User.Any(x => x.Login == registrationData.Login);

                if (!userExists)
                {
                    User user = new User()
                    {
                        Login = registrationData.Login,
                        Password = registrationData.Password,
                        FirstName = registrationData.FirstName,
                        LastName = registrationData.LastName,
                        Email = registrationData.Email
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
            return View();
        }

        //
        // POST: /Account/SignIn
        [HttpPost]
        [AllowAnonymous]
        public ActionResult SignIn(SignInViewModel credentials)
        {
            if (ModelState.IsValid)
            {
                User user = context.User.SingleOrDefault(x => x.Login == credentials.Login && x.Password == credentials.Password);
                if (user != null) {
                    FormsAuthentication.SetAuthCookie(credentials.Login, credentials.RememberMe);
                    Session["UserID"] = user.UserID;
                    Session["UserLogin"] = credentials.Login;

                    return RedirectToAction("Index", "Chat");
                }
                else
                {
                    ModelState.AddModelError("", "You have entered incorrect login or password.");
                }
            }

            return View(credentials);
        }
    }
}