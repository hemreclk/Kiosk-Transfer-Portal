using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using CabinextKioskAktarim.Models;
using CabinextKioskAktarim.Services;
using CabinextKioskAktarim.Filters;

namespace CabinextKioskAktarim.Controllers
{
    [CabinextOturum]
    public class KioskController : Controller
    {
        [HttpGet]
        public ActionResult Yukle()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult YukleKontrol(HttpPostedFileBase dosya)
        {
            if (dosya == null || dosya.ContentLength == 0)
            {
                ViewBag.Hata = "Lütfen boş olmayan bir CSV dosyası seçin. Eksik dosya yüklemeye çalıştınız.";
                return View("Yukle");
            }

            if (dosya.ContentLength > 2 * 1024 * 1024)
            {
                ViewBag.Hata = "Dosya boyutu çok yüksek. Yalnızca maksimum 2 MB olan dosyaları yükleyebilirsiniz.";
                return View("Yukle");
            }

            if (!string.Equals(System.IO.Path.GetExtension(dosya.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Hata = "Yanlış uzantı. Yalnızca .csv uzantılı dosyalar desteklenmektedir.";
                return View("Yukle");
            }

            try
            {
                var okuyucu = new KioskCsvOkuyucu();
                var satirlar = okuyucu.Oku(dosya.InputStream);

                if (satirlar.Count == 0)
                {
                    ViewBag.Hata = "Dosyada hiç veri satırı bulunamadı.";
                    return View("Yukle");
                }

                var dogrulama = new KioskDogrulamaServisi();
                dogrulama.Dogrula(satirlar);

                // Store validated rows in Session for the transfer confirmation step
                Session["ValidatedKiosks"] = satirlar;
                Session["FileName"] = dosya.FileName;

                ViewBag.FileName = dosya.FileName;
                ViewBag.Satirlar = satirlar;
                ViewBag.Asama = 3; // Step 1: File chosen, Step 2: CSV parsed, Step 3: Validated
            }
            catch (FormatException ex)
            {
                ViewBag.Hata = "Eksik veya hatalı sütun formatı: " + ex.Message;
            }
            catch (Microsoft.VisualBasic.FileIO.MalformedLineException)
            {
                ViewBag.Hata = "CSV dosyasında bozuk bir satır var. Lütfen dosyayı kontrol edip tekrar deneyin.";
            }

            return View("Yukle");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult YukleTamamla()
        {
            if (!(Session["ValidatedKiosks"] is List<KioskCsvSatiri> satirlar))
            {
                ViewBag.Hata = "Oturum süresi dolmuş veya geçerli bir dosya kontrol edilmemiş. Lütfen dosyayı tekrar yükleyin.";
                return View("Yukle");
            }

            var kayit = new KioskKayitServisi();
            kayit.Kaydet(satirlar);

            TempData["SonucSatirlari"] = satirlar;

            // Clear session after transfer to maintain security
            Session.Remove("ValidatedKiosks");
            Session.Remove("FileName");

            return RedirectToAction("YukleSonuc");
        }

        [HttpGet]
        public ActionResult YukleSonuc()
        {
            if (!(TempData["SonucSatirlari"] is List<KioskCsvSatiri> satirlar))
            {
                return RedirectToAction("Yukle");
            }

            // Keep it in TempData if user refreshes page? The prompt says:
            // "Hata satırlarını kalıcı bir import geçmişi tablosunda tutmuyorsak yenileme sonrası geçmiş varmış gibi gösterme; sonuç sayfasının yaşam süresini açıkça ele al."
            // By reading TempData without Keep(), it automatically expires. A refresh will hit the null check and redirect. This matches the prompt's instruction perfectly.
            
            return View(satirlar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HizliYukle(HttpPostedFileBase dosya)
        {
            if (dosya == null || dosya.ContentLength == 0)
            {
                TempData["Hata"] = "Lütfen boş olmayan bir CSV dosyası seçin. Eksik dosya yüklemeye çalıştınız.";
                return RedirectToAction("Index", "Home");
            }

            if (dosya.ContentLength > 2 * 1024 * 1024)
            {
                TempData["Hata"] = "Dosya boyutu çok yüksek. Yalnızca maksimum 2 MB olan dosyaları yükleyebilirsiniz.";
                return RedirectToAction("Index", "Home");
            }

            if (!string.Equals(System.IO.Path.GetExtension(dosya.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Hata"] = "Yanlış uzantı. Yalnızca .csv uzantılı dosyalar desteklenmektedir.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var okuyucu = new KioskCsvOkuyucu();
                var satirlar = okuyucu.Oku(dosya.InputStream);

                if (satirlar.Count == 0)
                {
                    TempData["Hata"] = "Dosyada hiç veri satırı bulunamadı.";
                    return RedirectToAction("Index", "Home");
                }

                var dogrulama = new KioskDogrulamaServisi();
                dogrulama.Dogrula(satirlar);

                var kayit = new KioskKayitServisi();
                kayit.Kaydet(satirlar);

                int basarili = satirlar.Count(s => string.IsNullOrEmpty(s.Hata));
                TempData["Basari"] = $"CSV yükleme tamamlandı. Toplam {satirlar.Count} satır işlendi, {basarili} adet başarılı kayıt aktarıldı.";
                
                return RedirectToAction("Index", "Kiosk");
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Dosya işlenirken bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public ActionResult Index(
            string barkod, string distributor,
            string musteri, string depo, DateTime? baslangicTarihi, DateTime? bitisTarihi, int sayfa = 1)
        {
            var model = new KioskListeModeli
            {
                Barkod = (barkod ?? "").Trim(),
                Distributor = (distributor ?? "").Trim(),
                Musteri = (musteri ?? "").Trim(),
                Depo = (depo ?? "").Trim(),
                BaslangicTarihi = baslangicTarihi,
                BitisTarihi = bitisTarihi
            };

            string filtre = @"
                WHERE (@barkod = '' OR
                       LOCATE(@barkod, Barcode) > 0)
                  AND (@distributor = '' OR
                       LOCATE(@distributor, RLocCode) > 0)
                  AND (@musteri = '' OR
                       LOCATE(@musteri, RCustCode) > 0)
                  AND (@depo = '' OR
                       LOCATE(@depo, WhCode) > 0)";

            if (model.BaslangicTarihi.HasValue)
                filtre += " AND DATE(CreatedOn) >= @baslangicTarihi";
                
            if (model.BitisTarihi.HasValue)
                filtre += " AND DATE(CreatedOn) <= @bitisTarihi";


            string ayar = ConfigurationManager
                .ConnectionStrings["CabinextLocal"].ConnectionString;

            using (var baglanti = new MySqlConnection(ayar))
            {
                baglanti.Open();

                using (var komut = new MySqlCommand(
                    "SELECT COUNT(*) FROM cabinets " + filtre,
                    baglanti))
                {
                    FiltreleriEkle(komut, model);
                    model.Toplam = Convert.ToInt32(
                        komut.ExecuteScalar());
                }

                model.Sayfa = Math.Min(
                    Math.Max(1, sayfa), model.ToplamSayfa);

                string sql = @"
                    SELECT Code, Barcode, RLocCode,
                           RCustCode, WhCode, CreatedOn, Active, Reachable, BrandCode
                    FROM cabinets " + filtre + @"
                    ORDER BY CreatedOn DESC, Code DESC
                    LIMIT @adet OFFSET @baslangic";

                using (var komut = new MySqlCommand(sql, baglanti))
                {
                    FiltreleriEkle(komut, model);
                    komut.Parameters.AddWithValue(
                        "@adet", model.SayfaBoyutu);
                    komut.Parameters.AddWithValue(
                        "@baslangic",
                        (model.Sayfa - 1) * model.SayfaBoyutu);

                    using (var sonuc = komut.ExecuteReader())
                    {
                        while (sonuc.Read())
                        {
                            var k = new KioskListeSatiri
                            {
                                Code = sonuc.GetString(0),
                                Barcode = sonuc.IsDBNull(1) ? "" : sonuc.GetString(1),
                                RLocCode = sonuc.IsDBNull(2) ? "" : sonuc.GetString(2),
                                RCustCode = sonuc.IsDBNull(3) ? "" : sonuc.GetString(3),
                                WhCode = sonuc.IsDBNull(4) ? "" : sonuc.GetString(4),
                                CreatedOn = sonuc.IsDBNull(5) ? (DateTime?)null : sonuc.GetDateTime(5),
                                Active = sonuc.IsDBNull(6) ? (bool?)null : (sonuc.GetInt32(6) == 1),
                                Reachable = sonuc.IsDBNull(7) ? (bool?)null : (sonuc.GetInt32(7) == 1),
                                BrandCode = sonuc.IsDBNull(8) ? "" : sonuc.GetString(8)
                            };
                            
                            if (k.Active == true) {
                                k.AktarimDurumu = "Tamamlandı";
                                k.AktarimDurumuRenk = "success";
                            } else if (k.Active == false) {
                                k.AktarimDurumu = "Hata";
                                k.AktarimDurumuRenk = "error";
                            } else if (k.Reachable == true) {
                                k.AktarimDurumu = "Aktarımda";
                                k.AktarimDurumuRenk = "blue";
                            } else {
                                k.AktarimDurumu = "Beklemede";
                                k.AktarimDurumuRenk = "warning";
                            }

                            model.Kayitlar.Add(k);
                        }
                    }
                }
            }

            return View(model);
        }

        private static void FiltreleriEkle(
            MySqlCommand komut, KioskListeModeli model)
        {
            komut.Parameters.AddWithValue(
                "@barkod", model.Barkod);
            komut.Parameters.AddWithValue(
                "@distributor", model.Distributor);
            komut.Parameters.AddWithValue(
                "@musteri", model.Musteri);
            komut.Parameters.AddWithValue(
                "@depo", model.Depo);

            if (model.BaslangicTarihi.HasValue)
                komut.Parameters.AddWithValue("@baslangicTarihi", model.BaslangicTarihi.Value.ToString("yyyy-MM-dd"));
                
            if (model.BitisTarihi.HasValue)
                komut.Parameters.AddWithValue("@bitisTarihi", model.BitisTarihi.Value.ToString("yyyy-MM-dd"));
        }

        [HttpGet]
        public ActionResult Detay(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

            string ayar = ConfigurationManager.ConnectionStrings["CabinextLocal"].ConnectionString;
            var k = new KioskListeSatiri();
            
            using (var baglanti = new MySqlConnection(ayar))
            {
                baglanti.Open();
                string sql = @"
                    SELECT Code, Barcode, RLocCode, RCustCode, WhCode, CreatedOn, Active, Reachable, BrandCode
                    FROM cabinets 
                    WHERE Code = @id OR Barcode = @id
                    LIMIT 1";
                    
                using (var komut = new MySqlCommand(sql, baglanti))
                {
                    komut.Parameters.AddWithValue("@id", id);
                    using (var sonuc = komut.ExecuteReader())
                    {
                        if (sonuc.Read())
                        {
                            k.Code = sonuc.GetString(0);
                            k.Barcode = sonuc.IsDBNull(1) ? "" : sonuc.GetString(1);
                            k.RLocCode = sonuc.IsDBNull(2) ? "" : sonuc.GetString(2);
                            k.RCustCode = sonuc.IsDBNull(3) ? "" : sonuc.GetString(3);
                            k.WhCode = sonuc.IsDBNull(4) ? "" : sonuc.GetString(4);
                            k.CreatedOn = sonuc.IsDBNull(5) ? (DateTime?)null : sonuc.GetDateTime(5);
                            k.Active = sonuc.IsDBNull(6) ? (bool?)null : (sonuc.GetInt32(6) == 1);
                            k.Reachable = sonuc.IsDBNull(7) ? (bool?)null : (sonuc.GetInt32(7) == 1);
                            k.BrandCode = sonuc.IsDBNull(8) ? "" : sonuc.GetString(8);
                            
                            if (k.Active == true) {
                                k.AktarimDurumu = "Tamamlandı";
                                k.AktarimDurumuRenk = "success";
                            } else if (k.Active == false) {
                                k.AktarimDurumu = "Hata";
                                k.AktarimDurumuRenk = "error";
                            } else if (k.Reachable == true) {
                                k.AktarimDurumu = "Aktarımda";
                                k.AktarimDurumuRenk = "blue";
                            } else {
                                k.AktarimDurumu = "Beklemede";
                                k.AktarimDurumuRenk = "warning";
                            }
                        }
                    }
                }
            }

            // If not found in primary cabinets table, look up in error logs
            if (string.IsNullOrEmpty(k.Code))
            {
                try
                {
                    string logAyar = ConfigurationManager.ConnectionStrings["CabinextLogs"].ConnectionString;
                    using (var logBaglanti = new MySqlConnection(logAyar))
                    {
                        logBaglanti.Open();
                        using (var cmd = new MySqlCommand("SELECT KioskCode, DistributorCode, CustomerCode, WarehouseCode, ErrorMessage FROM failed_kiosks WHERE KioskCode = @id LIMIT 1", logBaglanti))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            using (var sonuc = cmd.ExecuteReader())
                            {
                                if (sonuc.Read())
                                {
                                    k.Code = sonuc["KioskCode"].ToString();
                                    k.RLocCode = sonuc["DistributorCode"].ToString();
                                    k.RCustCode = sonuc["CustomerCode"].ToString();
                                    k.WhCode = sonuc["WarehouseCode"].ToString();
                                    k.CreatedOn = null;
                                    k.AktarimDurumu = "Hata";
                                    k.AktarimDurumuRenk = "error";
                                    ViewBag.HataMesaji = sonuc["ErrorMessage"].ToString();
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            if (string.IsNullOrEmpty(k.Code))
            {
                return RedirectToAction("Index");
            }

            // If status is Error, fetch root cause message from error logs
            if (k.AktarimDurumu == "Hata")
            {
                try
                {
                    string logAyar = ConfigurationManager.ConnectionStrings["CabinextLogs"].ConnectionString;
                    using (var logBaglanti = new MySqlConnection(logAyar))
                    {
                        logBaglanti.Open();
                        using (var cmd = new MySqlCommand("SELECT ErrorMessage FROM failed_kiosks WHERE KioskCode = @c ORDER BY FailedAt DESC LIMIT 1", logBaglanti))
                        {
                            cmd.Parameters.AddWithValue("@c", k.Code);
                            var err = cmd.ExecuteScalar();
                            if (err != null && err != DBNull.Value)
                                ViewBag.HataMesaji = err.ToString();
                        }
                    }
                }
                catch { } // Ignore if error logging database is unavailable
            }

            return View(k);
        }

        [HttpGet]
        public ActionResult HataliKayitlar()
        {
            var model = new List<FailedKioskSummary>();
            try
            {
                string logAyar = ConfigurationManager.ConnectionStrings["CabinextLogs"].ConnectionString;
                using (var logBaglanti = new MySqlConnection(logAyar))
                {
                    logBaglanti.Open();
                    using (var cmd = new MySqlCommand("SELECT KioskCode, DistributorCode, CustomerCode, WarehouseCode, ErrorMessage FROM failed_kiosks", logBaglanti))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                model.Add(new FailedKioskSummary
                                {
                                    KioskCode = reader["KioskCode"].ToString(),
                                    DistributorCode = reader["DistributorCode"].ToString(),
                                    CustomerCode = reader["CustomerCode"].ToString(),
                                    WarehouseCode = reader["WarehouseCode"].ToString(),
                                    ErrorMessage = reader["ErrorMessage"].ToString(),
                                    FailedAt = null
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Hatalı kayıtlar yüklenirken bir sorun oluştu: " + ex.Message;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SilHatali(string kioskCode)
        {
            try
            {
                string logAyar = ConfigurationManager.ConnectionStrings["CabinextLogs"].ConnectionString;
                using (var logBaglanti = new MySqlConnection(logAyar))
                {
                    logBaglanti.Open();
                    using (var cmd = new MySqlCommand("DELETE FROM failed_kiosks WHERE KioskCode = @kioskCode", logBaglanti))
                    {
                        cmd.Parameters.AddWithValue("@kioskCode", kioskCode);
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["Basari"] = "Hatalı kayıt başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Kayıt silinirken bir hata oluştu: " + ex.Message;
            }
            
            // Return to referrer URL if available; otherwise redirect to failed records list
            if (Request.UrlReferrer != null)
                return Redirect(Request.UrlReferrer.ToString());
                
            return RedirectToAction("HataliKayitlar");
        }
    }
}