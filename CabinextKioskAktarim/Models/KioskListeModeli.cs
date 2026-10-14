using System;
using System.Collections.Generic;

namespace CabinextKioskAktarim.Models
{
    public class KioskListeSatiri
    {
        public string Code { get; set; }
        public string Barcode { get; set; }
        public string RLocCode { get; set; }
        public string RCustCode { get; set; }
        public string WhCode { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool? Active { get; set; }
        public bool? Reachable { get; set; }
        public string BrandCode { get; set; }

        public string AktarimDurumu { get; set; }
        public string AktarimDurumuRenk { get; set; }
    }

    public class KioskListeModeli
    {
        public List<KioskListeSatiri> Kayitlar { get; set; }
            = new List<KioskListeSatiri>();

        public string Barkod { get; set; }
        public string Distributor { get; set; }
        public string Musteri { get; set; }
        public string Depo { get; set; }
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }

        public int Sayfa { get; set; }
        public int Toplam { get; set; }
        public int SayfaBoyutu { get; set; } = 20;

        public int ToplamSayfa
        {
            get
            {
                return Math.Max(1,
                    (int)Math.Ceiling(
                        (double)Toplam / SayfaBoyutu));
            }
        }
    }
}