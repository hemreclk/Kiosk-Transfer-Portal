using System;
using System.Web;
using System.Web.Mvc;

namespace CabinextKioskAktarim.Filters
{
    public class CabinextOturumAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase context)
        {
            if (!base.AuthorizeCore(context))
                return false;

            object bitis = context.Session["CabinextTokenExpiresUtc"];
            return context.Session["CabinextAccessToken"] is string &&
                   bitis is DateTime &&
                   (DateTime)bitis > DateTime.UtcNow;
        }
    }
}
