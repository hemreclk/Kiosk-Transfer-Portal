using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CabinextKioskAktarim.Models;
using Newtonsoft.Json;

namespace CabinextKioskAktarim.Services
{
    public class CabinextApiGirisServisi
    {
        private static readonly HttpClient Client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        public async Task<CabinextTokenYaniti> GirisYapAsync(
            string kullaniciAdi, string sifre)
        {
            
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            string adres = ConfigurationManager
                .AppSettings["CabinextTokenUrl"];
            string clientId = ConfigurationManager
                .AppSettings["CabinextClientId"];

            var alanlar = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", kullaniciAdi },
                { "password", sifre },
                { "client_id", clientId }
            };

            using (var icerik = new FormUrlEncodedContent(alanlar))
            using (var yanit = await Client.PostAsync(adres, icerik))
            {
                if (!yanit.IsSuccessStatusCode)
                {
                    string hataDetayi = await yanit.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"API Hatası ({yanit.StatusCode}): {hataDetayi}");
                }
                
                yanit.EnsureSuccessStatusCode();

                string json = await yanit.Content.ReadAsStringAsync();
                var token = JsonConvert
                    .DeserializeObject<CabinextTokenYaniti>(json);

                if (token == null ||
                    string.IsNullOrWhiteSpace(token.AccessToken))
                    throw new InvalidOperationException(
                        "API geçerli bir erişim anahtarı göndermedi.");

                return token;
            }
        }
    }
}
