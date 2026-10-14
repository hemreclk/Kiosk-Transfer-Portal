using System;
using System.Collections.Generic;
using System.Configuration;
using CabinextKioskAktarim.Models;
using MySql.Data.MySqlClient;

namespace CabinextKioskAktarim.Services
{
    public class KioskKayitServisi
    {
        public void Kaydet(List<KioskCsvSatiri> satirlar)
        {
            string ayar = ConfigurationManager
                .ConnectionStrings["CabinextLocal"].ConnectionString;

            using (var baglanti = new MySqlConnection(ayar))
            {
                baglanti.Open();

                foreach (var satir in satirlar)
                {
                    if (satir.Hata != null)
                        continue;

                    if (string.IsNullOrWhiteSpace(satir.DepoCode) ||
                        satir.DepoCode.Length > 8)
                    {
                        satir.Hata =
                            "Depo kodu yok veya 8 karakterden uzun.";
                        continue;
                    }

                    using (var islem = baglanti.BeginTransaction())
                    {
                        try
                        {
                            KioskEkle(baglanti, islem, satir);

                            HareketEkle(baglanti, islem, satir,
                                1, "32", "HO", satir.DepoCode,
                                null, "Kiosk İlk kayıt");

                            HareketEkle(baglanti, islem, satir,
                                2, "22", satir.DepoCode,
                                satir.CustCode, satir.Dkodu,
                                "Kiosk ilk müşteri kaydı");

                            islem.Commit();
                            satir.OlusturulanKod = satir.KioskD;
                        }
                        catch (MySqlException ex)
                        {
                            islem.Rollback();
                            satir.Hata = ex.Number == 1062
                                ? "Kiosk kodu veya barkod zaten kayıtlı."
                                : "Veritabanına kayıt sırasında hata oluştu.";
                        }
                        catch (Exception)
                        {
                            islem.Rollback();
                            satir.Hata =
                                "Kiosk ve hareketler kaydedilemedi.";
                        }
                    } // using islem
                } // foreach
            } // using baglanti

            // Persist failed records to the dedicated audit log database
            try
            {
                HatalariLogla(satirlar);
            }
            catch
            {
                // Silently catch logging errors to prevent breaking the primary transaction flow
            }
        }

        private void HatalariLogla(List<KioskCsvSatiri> satirlar)
        {
            string logAyar = ConfigurationManager.ConnectionStrings["CabinextLogs"]?.ConnectionString;
            if (string.IsNullOrEmpty(logAyar)) return;

            using (var baglanti = new MySqlConnection(logAyar))
            {
                baglanti.Open();
                foreach (var satir in satirlar)
                {
                    if (satir.Hata != null)
                    {
                        const string sql = @"
                            INSERT INTO failed_kiosks 
                            (KioskCode, Barcode, DistributorCode, CustomerCode, WarehouseCode, ErrorMessage) 
                            VALUES 
                            (@kioskD, @kioskD, @dist, @musteri, @depo, @hata)";

                        using (var komut = new MySqlCommand(sql, baglanti))
                        {
                            komut.Parameters.AddWithValue("@kioskD", (object)satir.KioskD ?? DBNull.Value);
                            komut.Parameters.AddWithValue("@dist", (object)satir.Dkodu ?? DBNull.Value);
                            komut.Parameters.AddWithValue("@musteri", (object)satir.CustCode ?? DBNull.Value);
                            komut.Parameters.AddWithValue("@depo", (object)satir.DepoCode ?? DBNull.Value);
                            komut.Parameters.AddWithValue("@hata", satir.Hata);
                            komut.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private void KioskEkle(
            MySqlConnection baglanti, MySqlTransaction islem,
            KioskCsvSatiri satir)
        {
            const string sql = @"
                INSERT INTO cabinets
                (Code, Barcode, Year, TypeCode, FAssetNo,
                 Status, CabinetView, LastAction, Active, InDate,
                 Owner, RLocCode, RCustCode, WhCode,
                 CreatedBy, CreatedOn, UsageStatus, Reachable,
                 SubTypeCode, SenderCode, NfcStatus, BrandCode)
                VALUES
                (@kioskD, @kioskD, @yil, '140', @kioskD,
                 '01', 3, '22', 1, NOW(),
                 0, @dist, @musteri, @depo,
                 1, NOW(), 0, 1,
                 '239', 'HO', 0, 'MGM')";

            using (var komut = new MySqlCommand(sql, baglanti, islem))
            {
                komut.Parameters.AddWithValue("@kioskD", satir.KioskD);
                komut.Parameters.AddWithValue("@yil", DateTime.Now.Year);
                komut.Parameters.AddWithValue("@dist", satir.Dkodu);
                komut.Parameters.AddWithValue(
                    "@musteri", satir.CustCode);
                komut.Parameters.AddWithValue(
                    "@depo", satir.DepoCode);

                komut.ExecuteNonQuery();
            }
        }

        private void HareketEkle(
            MySqlConnection baglanti, MySqlTransaction islem,
            KioskCsvSatiri satir, int sira,
            string hareketTuru, string gonderen,
            string alan, string distribütor, string aciklama)
        {
            const string sql = @"
                INSERT INTO cabinetmovements
                (Code, Barcode, LineNumber, MovementType,
                 Sender, Receiver, RLocCode, Date,
                 Description, CreatedBy, CreatedOn)
                VALUES
                (@kioskD, @kioskD, @sira, @tur,
                 @gonderen, @alan, @dist, NOW(),
                 @aciklama, 1, NOW())";

            using (var komut = new MySqlCommand(sql, baglanti, islem))
            {
                komut.Parameters.AddWithValue("@kioskD", satir.KioskD);
                komut.Parameters.AddWithValue("@sira", sira);
                komut.Parameters.AddWithValue("@tur", hareketTuru);
                komut.Parameters.AddWithValue("@gonderen", gonderen);
                komut.Parameters.AddWithValue("@alan", alan);
                komut.Parameters.AddWithValue(
                    "@dist", (object)distribütor ?? DBNull.Value);
                komut.Parameters.AddWithValue("@aciklama", aciklama);

                komut.ExecuteNonQuery();
            }
        }
    }
}