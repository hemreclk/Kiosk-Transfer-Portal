using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CabinextKioskAktarim.Services;
using CabinextKioskAktarim.Filters;

namespace CabinextKioskAktarim.Controllers
{
    [CabinextOturum]
    public class HomeController : Controller
    {
        public ActionResult DbKontrol()
        {
            string ayar = System.Configuration.ConfigurationManager
                .ConnectionStrings["CabinextLocal"].ConnectionString;

            using (var baglanti =
                new MySql.Data.MySqlClient.MySqlConnection(ayar))
            {
                baglanti.Open();
                return Content("Yerel veritabanı bağlantısı başarılı.");
            }
        }

        public ActionResult Index()
        {
            var service = new DashboardService();
            var model = service.GetDashboardData();
            return View(model);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}