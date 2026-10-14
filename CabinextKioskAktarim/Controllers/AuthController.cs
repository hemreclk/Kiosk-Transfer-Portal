using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Configuration;
using CabinextKioskAktarim.Models;
using CabinextKioskAktarim.Services;

namespace CabinextKioskAktarim.Controllers
{
    public class AuthController : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                if (Session["CabinextAccessToken"] != null)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // Session expired but cookie remains; clear cookie to prevent redirect loop
                    // Redirect to refresh login context and avoid AntiForgery mismatch errors
                    FormsAuthentication.SignOut();
                    return RedirectToAction("Login", new { returnUrl = returnUrl });
                }
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            
            string configuredUser = ConfigurationManager.AppSettings["AdminUsername"];
            string configuredPass = ConfigurationManager.AppSettings["AdminPassword"];

            // Fallback for local development if not explicitly configured in Web.config
            if (string.IsNullOrEmpty(configuredUser)) configuredUser = "admin";
            if (string.IsNullOrEmpty(configuredPass)) configuredPass = "Admin123!";

            bool isAuthorized = string.Equals(model.Username?.Trim(), configuredUser, StringComparison.OrdinalIgnoreCase)
                                && model.Password?.Trim() == configuredPass;

            if (isAuthorized)
            {
                Session["CabinextAccessToken"] = Guid.NewGuid().ToString("N");
                Session["CabinextTokenExpiresUtc"] = DateTime.UtcNow.AddDays(1);
                Session["CabinextFullName"] = model.Username?.Trim();

                FormsAuthentication.SetAuthCookie(model.Username?.Trim(), false);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult ChangeLanguage(string lang, string returnUrl)
        {
            if (!string.IsNullOrEmpty(lang))
            {
                HttpCookie cookie = new HttpCookie("Language");
                cookie.Value = lang;
                cookie.Expires = DateTime.Now.AddYears(1);
                Response.Cookies.Add(cookie);
            }
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Login");
        }
    }
}
