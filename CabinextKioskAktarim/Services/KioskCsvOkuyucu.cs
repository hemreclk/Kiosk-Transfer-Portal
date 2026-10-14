using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CabinextKioskAktarim.Models;
using Microsoft.VisualBasic.FileIO;

namespace CabinextKioskAktarim.Services
{
    public class KioskCsvOkuyucu
    {
        public List<KioskCsvSatiri> Oku(Stream dosyaAkisi)
        {
            var satirlar = new List<KioskCsvSatiri>();

            using (var csv = new TextFieldParser(
                dosyaAkisi, Encoding.UTF8, true))
            {
                csv.SetDelimiters(",");
                csv.HasFieldsEnclosedInQuotes = true;

                string[] beklenen =
                    { "Dkodu", "MKodu", "KabinD",
                      "KioskD", "CustCode" };

                string[] baslik = csv.ReadFields();

                if (baslik == null ||
                    !baslik.SequenceEqual(beklenen))
                {
                    throw new FormatException(
                        "CSV başlıkları eksik veya sırası farklı.");
                }

                int satirNo = 1; // Header line

                while (!csv.EndOfData)
                {
                    satirNo++;
                    string[] alanlar = csv.ReadFields();

                    if (alanlar == null || alanlar.Length != 5)
                    {
                        throw new FormatException(
                            satirNo + ". satırda 5 sütun bulunmalı.");
                    }

                    satirlar.Add(new KioskCsvSatiri
                    {
                        SatirNo = satirNo,
                        Dkodu = alanlar[0].Trim(),
                        MKodu = alanlar[1].Trim(),
                        KabinD = alanlar[2].Trim(),
                        KioskD = alanlar[3].Trim(),
                        CustCode = alanlar[4].Trim()
                    });
                }
            }

            return satirlar;
        }
    }
}