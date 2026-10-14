# Cabinext Kiosk Aktarım Portalı

**Cabinext Kiosk Aktarım**, binlerce kiosk cihazının sisteme toplu olarak kaydedilmesi ve bu cihazların merkezden depolara, depolardan da müşterilere sevk (hareket) işlemlerinin otomatikleştirilmesi amacıyla geliştirilmiş modern ve web tabanlı bir operasyon yönetim panelidir.

## 🚀 Proje Özeti
Bu sistem, yetkili kullanıcıların CSV dosyası yükleyerek karmaşık ve zaman alıcı "Kiosk Tanımlama" ve "Depo/Müşteri Transferi" işlemlerini tek bir tıklama ile yapabilmesini sağlar. İşlem sonunda başarı, hata ve tekrar eden kayıt istatistiklerini net bir şekilde sunarak operasyonel hata payını sıfıra indirir.

---

## ✨ Öne Çıkan Özellikler

1. **Toplu Veri Aktarımı (CSV ile):** 
   - Kullanıcılar, Excel'den dışa aktarılmış `.csv` formatındaki cihaz listelerini sisteme saniyeler içinde yükleyebilir.
   - Sürükle-bırak (Drag & Drop) destekli modern yükleme alanı.
   
2. **Kapsamlı Sonuç ve Hata Yönetimi Raporlaması:**
   - Aktarım tamamlandığında kullanıcı; toplam okunan satır sayısını, başarıyla oluşturulanları, tekrar eden veya doğrulama hatasına takılan cihazları özet bilgi kartlarında (Dashboard mantığıyla) görür.
   - Hatalı kayıtlar için anında uyarı veren ve inceleme şansı sunan detay ekranları.

3. **Çoklu Dil Desteği (Multi-language):**
   - Uygulama anlık olarak **Türkçe, İngilizce (English), Azerbaycan Türkçesi (Azərbaycan) ve Rusça (Русский)** dillerini destekler.
   - Kullanıcı dil seçtiğinde sayfayı yenilemeden Session ve Cookie altyapısı ile tüm sistem dili entegre olarak değişir.

4. **Kusursuz UI/UX (Kullanıcı Deneyimi):**
   - **Cabinext Kurumsal Kimliği:** Logolar, marka renkleri (Cabinext Yeşili & Kırmızısı) ve özel illüstrasyonlarla donatılmış premium bir arayüz.
   - **Modern Login Ekranı:** Tam sayfa arka plan görselleri, estetik gölgelendirmeler, temiz hata mesajları ve yalın form tasarımları.
   - **Responsive (Duyarlı) Tasarım:** Mobil, tablet ve masaüstü bilgisayarlarla uyumlu CSS Flexbox/Grid yapıları.

5. **Güvenlik ve Oturum Yönetimi:**
   - Korumalı sayfalara yetkisiz erişimi engellemek için özelleştirilmiş `[CabinextOturum]` filtresi.
   - Forms Authentication ve Anti-Forgery Tokens ile sahtecilik (CSRF) koruması.

---

## 🛠️ Kullanılan Teknolojiler ve Mimari

Proje, güçlü ve güvenilir Microsoft teknolojileri üzerine inşa edilmiş, modern Frontend mimarileriyle desteklenmiştir.

### ⚙️ Backend (Sunucu Tarafı)
- **Framework:** .NET Framework 4.7.2 / ASP.NET MVC 5
- **Programlama Dili:** C# 10+
- **Veritabanı Sürücüsü:** `MySql.Data` (v26.7.0) - Doğrudan MySQL bağlantıları ve veri çekme işlemleri için.
- **Veri İşleme:** `Newtonsoft.Json` (v13.0.3) - REST tabanlı dış servisler ile hızlı ve güvenli JSON veri alışverişi.
- **Güvenlik & Kriptografi:** `BouncyCastle.Cryptography`, LZ4 algoritmaları.
- **Katmanlı Mimari:** 
  - `Controllers`: Gelen isteklerin işlendiği (Auth, Home, Kiosk) sınıflar.
  - `Services`: İş mantığının (Dashboard, Kiosk Parse, Localization) yürütüldüğü katman.
  - `Filters`: Oturum ve yetki kontrolleri için Custom Action Filter'lar.

### 🎨 Frontend (İstemci Tarafı)
- **UI Framework:** Bootstrap 5.2.3 (Özelleştirilmiş)
- **Stil Yönetimi:** Vanilla CSS3. Global değişkenler `var(--brand-green)` ile tek noktadan tema kontrolü (`Site.css`).
- **İkonlar & Görseller:** 
  - Hafif ve yüksek çözünürlüklü SVG ikonlar.
  - Dil seçenekleri için `flag-icons` kütüphanesi.
- **JavaScript & Etkileşimler:** `jQuery` (v3.7.0) - asenkron UI geçişleri için.

---

## 💡 İşlem Akışı (Workflow)

1. **Giriş (Login):** Yetkili kullanıcı tanımlı yönetici bilgileriyle sisteme giriş yapar.
2. **Dashboard:** Son eklenen kioskların, hata oranlarının yer aldığı özet bir panel sunulur.
3. **CSV Yükleme:** Sol menüden "Kiosk Aktar" sayfasına geçilir. Dosya sisteme yüklenir.
4. **Veri Ayrıştırma & API Aktarımı:** Sistem arka planda CSV'yi okur, cihazlar için kayıt açar ve cihazın depo/bayi hareket belgelerini otomatik oluşturur.
5. **Sonuç Raporlaması:** Hangi cihazların başarılı olduğu, hangilerinin hataya düştüğü modern bir zaman çizelgesi (timeline) formunda özetlenir.
