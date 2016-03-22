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
        private int[] timeouts = { 5, 10, 15, 20, 25, 30 };

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
                bool userExists = context.User.Any(x => x.Login == registrationData.Login);

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
                        IdleTimeout = 5
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
            if (FormsAuthentication.CookiesSupported)
            {
                var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
                if (authCookie != null && Session["UserId"] != null)
                {
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

                    context.SaveChanges();

                    FormsAuthentication.SetAuthCookie(credentials.Login, credentials.RememberMe);
                    Session["UserId"] = user.UserId;
                    Session["UserLogin"] = user.Login;
                    Session.Timeout = user.IdleTimeout;

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
        // GET: /Account/EditProfile
        public ActionResult EditProfile()
        {
            if (Session["UserId"] != null)
            {
                var userId = (Guid)Session["UserId"];
                User user = context.User.SingleOrDefault(x => x.UserId == userId);
                ProfileEditViewModel profileData = new ProfileEditViewModel()
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    IdleTimeout = user.IdleTimeout
                };

                ViewBag.IdleTimeout = new SelectList(timeouts.Select(x => new SelectListItem()
                {
                    Value = x.ToString(),
                    Text = x.ToString()
                }).ToList(), "Value", "Text", profileData.IdleTimeout);

                return View(profileData);
            }
            else {
                return RedirectToAction("SignIn");
            }
        }

        //
        // POST: /Account/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(ProfileEditViewModel profileData)
        {
            if (ModelState.IsValid)
            {
                if (Session["UserId"] != null)
                {
                    var userId = (Guid)Session["UserId"];
                    User user = context.User.SingleOrDefault(x => x.UserId == userId);
                    user.FirstName = profileData.FirstName;
                    user.LastName = profileData.LastName;
                    user.Email = profileData.Email;
                    user.IdleTimeout = profileData.IdleTimeout;

                    context.SaveChanges();

                    ViewBag.SuccessMessage = "Your profile data changes has been changed successfuly. Idle timeout changes will be applied after you reenter into your account.";
                }
                else {
                    return RedirectToAction("SignIn");
                }
            }

            ViewBag.IdleTimeout = new SelectList(timeouts.Select(x => new SelectListItem()
            {
                Value = x.ToString(),
                Text = x.ToString()
            }).ToList(), "Value", "Text", profileData.IdleTimeout);

            return View(profileData);
        }

        //
        // GET: /Account/ChangePassword
        public ActionResult ChangePassword()
        {
            if (Session["UserId"] != null)
            {
                return View();
            }
            else {
                return RedirectToAction("SignIn");
            }
        }

        //
        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel passwords)
        {
            if (ModelState.IsValid)
            {
                if (Session["UserId"] != null)
                {
                    var userId = (Guid)Session["UserId"];
                    User user = context.User.SingleOrDefault(x => x.UserId == userId);

                    if (user.Password == passwords.OldPassword)
                    {
                        user.Password = passwords.NewPassword;

                        context.SaveChanges();

                        ViewBag.SuccessMessage = "Your account password has been successfuly changed.";
                    }
                    else {
                        ModelState.AddModelError("", "Old password is incorrect.");
                    }
                }
                else {
                    return RedirectToAction("SignIn");
                }
            }
            return View();
        }

        //
        // GET: /Account/SignOut
        public ActionResult SignOut()
        {
            if (Session["UserId"] != null)
            {
                var userId = (Guid)Session["UserId"];
                User user = context.User.SingleOrDefault(x => x.UserId == userId);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastActivityDate = DateTime.Now;
                }

                context.SaveChanges();
            }

            FormsAuthentication.SignOut();
            Session.Abandon();

            return RedirectToAction("SignIn");
        }
    }
}