using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using CabinextKioskAktarim.Models;
using MySql.Data.MySqlClient;

namespace CabinextKioskAktarim.Services
{
    public class KioskDogrulamaServisi
    {
        public void Dogrula(List<KioskCsvSatiri> satirlar)
        {
            string ayar = ConfigurationManager
                .ConnectionStrings["CabinextLocal"].ConnectionString;

            using (var baglanti = new MySqlConnection(ayar))
            {
                baglanti.Open();

                var tekrarEdenler = new HashSet<string>(
                    satirlar
                        .GroupBy(s => s.KioskD, StringComparer.OrdinalIgnoreCase)
                        .Where(grup => !string.IsNullOrWhiteSpace(grup.Key)
                                       && grup.Count() > 1)
                        .Select(grup => grup.Key),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var satir in satirlar)
                {
                    if (string.IsNullOrWhiteSpace(satir.Dkodu) ||
                        string.IsNullOrWhiteSpace(satir.CustCode) ||
                        string.IsNullOrWhiteSpace(satir.KioskD))
                    {
                        satir.Hata =
                            "Distribütör, müşteri veya barkod boş.";
                        continue;
                    }

                    if (satir.KioskD.Length > 12 ||
                        satir.Dkodu.Length > 8 ||
                        satir.CustCode.Length > 8)
                    {
                        satir.Hata =
                            "Barkod en fazla 13; distribütör ve müşteri " +
                            "kodları en fazla 8 karakter olmalı.";
                        continue;
                    }

                    if (tekrarEdenler.Contains(satir.KioskD))
                    {
                        satir.Hata =
                            "Bu barkod CSV dosyasında birden fazla satırda var.";
                        continue;
                    }

                    string mevcutKod = KayitliKioskKodu(
                        baglanti, satir.KioskD);

                    if (mevcutKod != null)
                    {
                        satir.Hata =
                            "Barkod daha önce kaydedilmiş. Kiosk kodu: " +
                            mevcutKod;
                        continue;
                    }

                    satir.Hata = KonumHatasi(
                        baglanti, satir.Dkodu, 1, "Distribütör");

                    if (satir.Hata != null)
                        continue;

                    satir.Hata = KonumHatasi(
                        baglanti, satir.CustCode, 5, "Müşteri");

                    if (satir.Hata != null)
                        continue;

                    string bagliDist = BagliUstKod(
                        baglanti, satir.CustCode, 1);

                    if (string.IsNullOrWhiteSpace(bagliDist))
                    {
                        satir.Hata =
                            "Müşterinin aktif distribütör bağlantısı yok.";
                        continue;
                    }

                    if (bagliDist != satir.Dkodu)
                    {
                        satir.Hata = "Müşteri " + bagliDist +
                            " distribütörüne bağlı; CSV'deki kod " +
                            satir.Dkodu + ".";
                        continue;
                    }

                    string depo = BagliUstKod(
                        baglanti, satir.Dkodu, 2);

                    if (string.IsNullOrWhiteSpace(depo))
                    {
                        satir.Hata =
                            "Distribütörün aktif depo bağlantısı yok.";
                        continue;
                    }

                    satir.DepoCode = depo;
                }
            }
        }

        private string KonumHatasi(
            MySqlConnection baglanti, string kod,
            int beklenenTur, string ad)
        {
            const string sql =
                "SELECT TypeCode, Status FROM locations " +
                "WHERE Code = @kod";

            using (var komut = new MySqlCommand(sql, baglanti))
            {
                komut.Parameters.AddWithValue("@kod", kod);

                using (var sonuc = komut.ExecuteReader())
                {
                    if (!sonuc.Read())
                        return ad + " bulunamadı: " + kod;

                    if (sonuc.IsDBNull(0) ||
                        sonuc.GetInt32(0) != beklenenTur)
                        return kod + " geçerli bir " +
                            ad.ToLowerInvariant() + " değil.";

                    if (sonuc.IsDBNull(1) ||
                        sonuc.GetInt32(1) != 1)
                        return ad + " aktif değil: " + kod;
                }
            }

            return null;
        }

        private string BagliUstKod(
            MySqlConnection baglanti, string altKod, int ustTur)
        {
            const string sql =
                "SELECT ParentPlaceCode FROM locationhierarchies " +
                "WHERE ChildPlaceCode = @altKod " +
                "AND ParentPlaceType = @ustTur AND Status = 1";

            using (var komut = new MySqlCommand(sql, baglanti))
            {
                komut.Parameters.AddWithValue("@altKod", altKod);
                komut.Parameters.AddWithValue("@ustTur", ustTur);

                using (var sonuc = komut.ExecuteReader())
                {
                    if (!sonuc.Read())
                        return null;

                    string ustKod = sonuc.IsDBNull(0)
                        ? null : sonuc.GetString(0);

                    if (sonuc.Read())
                        throw new InvalidOperationException(
                            altKod + " için birden fazla aktif üst bağlantı var.");

                    return ustKod;
                }
            }
        }

        private string KayitliKioskKodu(
            MySqlConnection baglanti, string barkod)
        {
            const string sql =
                "SELECT Code FROM cabinets " +
                "WHERE Barcode = @barkod LIMIT 1";

            using (var komut = new MySqlCommand(sql, baglanti))
            {
                komut.Parameters.AddWithValue("@barkod", barkod);
                object sonuc = komut.ExecuteScalar();

                return sonuc == null
                    ? null
                    : Convert.ToString(sonuc);
            }
        }
    }
}