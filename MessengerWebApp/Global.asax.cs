using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using MessengerWebApp.Models;

namespace MessengerWebApp
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        protected void Application_PostAuthenticateRequest(Object sender, EventArgs e) {
            if (FormsAuthentication.CookiesSupported) {
                var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
                if (authCookie != null) {
                    var login = FormsAuthentication.Decrypt(authCookie.Value).Name;                    
                    HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(login), new string[0]);
                }
            }
        }

        protected void Session_End(Object sender, EventArgs E)
        {
            MessengerWebAppDatabaseEntities context = new MessengerWebAppDatabaseEntities();
            var userId = (Guid)Session["UserId"];
            var user = context.User.SingleOrDefault(x => x.UserId == userId);
            user.IsOnline = false;
            user.LastActivityDate = DateTime.Now;
            context.SaveChanges();
        }
    }
}
