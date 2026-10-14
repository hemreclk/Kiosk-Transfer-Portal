# Cabinext Kiosk Transfer Portal
*(Bulk Kiosk Registration & Fleet Logistics Operations Portal)*

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![ASP.NET MVC](https://img.shields.io/badge/ASP.NET%20MVC-5.2.9-0078D7)](https://dotnet.microsoft.com/apps/aspnet/mvc)
[![C#](https://img.shields.io/badge/C%23-10.0-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![MySQL](https://img.shields.io/badge/MySQL-8.0%2B-4479A1?logo=mysql&logoColor=white)](https://www.mysql.com/)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.2.3-7952B3?logo=bootstrap&logoColor=white)](https://getbootstrap.com/)
[![jQuery](https://img.shields.io/badge/jQuery-3.7.0-0769AD?logo=jquery&logoColor=white)](https://jquery.com/)
[![Security](https://img.shields.io/badge/Security-Protected-success)](#-security--secret-management-github-guidelines)
[![i18n](https://img.shields.io/badge/i18n-TR%20%7C%20EN%20%7C%20AZ%20%7C%20RU-brightgreen)](#-internationalization---i18n)

---

## 📌 Project Overview & Purpose

**Cabinext Kiosk Transfer Portal** is an enterprise-grade web operation platform designed for bulk onboarding, registration, and automated logistical movement tracking (Head Office &rarr; Warehouse &rarr; Distributor &rarr; Customer) for thousands of kiosk terminal devices.

The system ingests hardware dispatch lists in Excel/CSV format, executes multi-tier validation checks, processes database transactions across a dual-database architecture with ACID compliance, isolates validation errors into dedicated audit logs, and equips operations teams with an interactive, real-time Executive Dashboard.

```
┌────────────────────────────────────────────────────────────────────────┐
│                        USER / OPERATIONS TEAM                          │
│                   (Web Interface / Drag & Drop CSV)                    │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ Upload CSV File (Max 2MB)
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                     CABINEXT KIOSK TRANSFER ENGINE                     │
│                                                                        │
│  [1. KioskCsvOkuyucu]         [2. KioskDogrulamaServisi]               │
│  TextFieldParser (UTF-8)  ──► • Format & Length Validation             │
│                               • In-file Duplicate Check                │
│                               • Hierarchy Validation (Dist->Cust->WH)  │
│                               • Database Barcode Duplicate Check       │
│                                                                        │
│  [3. KioskKayitServisi]                                                │
│  • Begin Database Transaction                                          │
│  • INSERT cabinets (Primary Kiosk Record)                              │
│  • INSERT cabinetmovements (1: HO -> Warehouse / Type: 32)             │
│  • INSERT cabinetmovements (2: Warehouse -> Customer / Type: 22)       │
│  • Commit / Automatic Rollback on Any Failure                          │
└───────────────────┬───────────────────────────────┬────────────────────┘
                    │                               │
           (Successful Records)              (Failed Records)
                    │                               │
                    ▼                               ▼
       ┌────────────────────────┐      ┌────────────────────────┐
       │   Primary Database     │      │   Audit Log Database   │
       │   (cabinext)           │      │   (cabinext_logs)      │
       │   • cabinets           │      │   • failed_kiosks      │
       │   • cabinetmovements   │      │   (Automatic 30-Day    │
       │   • locations          │      │    Retention Purge)    │
       │   • locationhierarchies│      │                        │
       └────────────────────────┘      └────────────────────────┘
```

---

## ✨ Key Features

### 1. 🚀 Batch Data Ingestion (CSV Processing)
* **Drag & Drop Upload Zone:** Accessible and responsive file uploader with visual drag-and-drop feedback.
* **Strict Format Verification:** Strictly permits only `.csv` files up to a maximum size of 2 MB.
* **Standards-Compliant Parser (`TextFieldParser`):** Full RFC-4180 compliance, safely handling quoted fields, leading zeros (e.g., `CustCode`), and UTF-8 encodings.
* **Multi-Stage Verification Pipeline:**
  * **Stage 1:** File selection and metadata pre-checks.
  * **Stage 2:** CSV stream reading and parsing into strongly-typed models.
  * **Stage 3:** Hierarchical business rule validation (Simulated in memory; zero database writes until confirmed).
  * **Stage 4:** Atomic transaction execution upon explicit user confirmation.

### 2. 🛡️ Transactional Safety & Data Integrity (ACID Compliance)
* **Atomic Batch Inserts:** For each valid kiosk, one primary cabinet entry (`cabinets`) and two logistic movement entries (`cabinetmovements`) are bound to a single `MySqlTransaction`.
* **Zero Partial Commits (Automated Rollback):** If any query fails, the entire transaction rolls back immediately; the database is never left in an inconsistent or partial state.
* **Dual-Database Fault Isolation:** Rows that fail validation do not abort the process for valid rows; failure reasons and original payloads are recorded into the dedicated `cabinext_logs` database.

### 3. 📊 Real-Time Executive Dashboard
* **Key Performance Indicators (KPIs):**
  * Total Registered Kiosks
  * Active and Delivered (`LastAction = '22'`) Kiosks
  * Unreachable / Connectivity-Loss (`Reachable = 0`) Kiosks
  * Total Active Customers and Storage Warehouses
* **Actionable Watchlists:** Instant view of the 5 most recently provisioned kiosks and hardware requiring operational attention.
* **Automated Log Maintenance:** Error records older than 30 days are automatically pruned in the background (`DATE_SUB(NOW(), INTERVAL 30 DAY)`).

### 4. 🔍 Kiosk Directory & Advanced Filtering
* **Server-Side Pagination:** Efficient paging (20 records per page) ensuring responsive queries even with high volumes.
* **Multi-Criteria Search:** Real-time filtering by Barcode, Distributor Code, Customer Code, Warehouse Code, and Creation Date ranges (`CreatedOn`).
* **Detailed Diagnostics & Error Views:** Quick inspection of hardware status and root-cause error messages from the audit table.

### 5. 🌐 Internationalization (i18n)
* **Supported Languages:** Turkish (`tr-TR`), English (`en-US`), Azerbaijani (`az-AZ`), and Russian (`ru-RU`).
* **Persistent Preferences:** Selected language is stored via HTTP cookies and synchronized to `Thread.CurrentThread.CurrentUICulture` inside `Global.asax.cs`.

### 6. 🔒 Enterprise Security & Session Management
* **Forms Authentication & CSRF Protection:** All state-changing POST endpoints are guarded with `@Html.AntiForgeryToken()`.
* **Configurable Credentials:** Authentication credentials are decoupled from application code and managed via configuration.
* **Custom Security Action Filter (`[CabinextOturum]`):** Validates active user sessions and token expiration timestamps before controller action execution.
* **Centralized API Integration Ready (`CabinextApiGirisServisi`):** Preconfigured HTTP client utilizing TLS 1.2 for OAuth password grant tokens against external Cabinext auth endpoints.

---

## 🛡️ Security & Secret Management (GitHub Guidelines)

This repository is structured to prevent secret leakage and comply with GitHub secret scanning standards:

1. **No Hardcoded Passwords or API Secrets:**
   * All administrative credentials, database connection strings, and external API URLs are managed through `Web.config` or environment variables.
   * Hardcoded credentials in source code have been replaced with dynamic configuration lookups.
2. **Template Configuration Provided (`Web.config.example`):**
   * A sanitized template [`Web.config.example`](file:///c:/Users/Casper/Desktop/cabinext/CabinextKioskAktarim/CabinextKioskAktarim/Web.config.example) is provided.
   * Real production connection strings, database passwords, and client keys must **never** be committed to public repositories.
3. **Comprehensive `.gitignore` Protection:**
   * Visual Studio local caches (`.vs/`), user preferences (`*.user`), build outputs (`bin/`, `obj/`), NuGet binary packages (`packages/`), scratch folders (`scratch/`), and local secrets (`*.secrets.config`) are excluded from Git tracking.

---

## 🛠️ Architecture & Technology Stack

| Layer | Technology / Library | Version | Description |
| :--- | :--- | :--- | :--- |
| **Backend Framework** | .NET Framework / ASP.NET MVC | 4.7.2 / 5.2.9 | High-performance enterprise MVC architecture |
| **Language** | C# | 10.0 | Strongly-typed service layer and models |
| **Database Engine** | MySQL | 8.0+ | Relational operational and analytical databases |
| **Database Driver** | `MySql.Data` | 26.7.0 | Direct high-throughput ADO.NET connection provider |
| **JSON Serialization** | `Newtonsoft.Json` | 13.0.3 | Fast JSON parsing for external API payloads |
| **Cryptography & Compression** | `BouncyCastle`, `K4os.Compression.LZ4` | 2.6.2 / 1.3.8 | Secure transport layer and stream compression |
| **Frontend Framework** | Bootstrap (Customized) | 5.2.3 | Responsive grid layout and components |
| **Client Scripting** | jQuery | 3.7.0 | DOM manipulation, AJAX, and form handling |
| **Design System** | Vanilla CSS3 Variables | - | Cabinext corporate design tokens and typography |

---

## 📁 Project Directory Structure

```text
CabinextKioskAktarim/
├── .gitignore                        # Git exclusion rules for VS, binaries, and local configs
├── CabinextKioskAktarim.sln          # Visual Studio Solution File
├── CabinextKioskAktarim_Tanitimi.md  # Original Turkish product overview document
├── README.md                         # Comprehensive technical documentation (This file)
├── requirements.txt                  # System, platform, and package requirements manifest
│
└── CabinextKioskAktarim/             # Primary Web Application Project
    ├── App_Start/                    # MVC Bootstrap Configurations
    │   ├── BundleConfig.cs           # Script and CSS bundle registries
    │   ├── FilterConfig.cs           # Global action filter registrations
    │   └── RouteConfig.cs            # URL route rules and default mappings
    │
    ├── Content/                      # Static Assets (Stylesheets & Images)
    │   ├── Images/                   # Corporate logos and background graphics
    │   ├── Site.css                  # Cabinext CSS custom design tokens
    │   └── bootstrap.min.css         # Bootstrap 5 layout styles
    │
    ├── Controllers/                  # MVC Application Controllers
    │   ├── AuthController.cs         # Authentication, sign-out, and language switching
    │   ├── HomeController.cs         # Executive dashboard and diagnostics
    │   └── KioskController.cs        # CSV ingestion, validation, listing, and details
    │
    ├── Filters/                      # Custom Security Filters
    │   └── CabinextOturumAttribute.cs# Session existence and token expiration filter
    │
    ├── Models/                       # Data Transfer and View Models
    │   ├── CabinextTokenYaniti.cs    # REST API authentication token response model
    │   ├── DashboardViewModel.cs     # Dashboard KPI counters and summary models
    │   ├── KioskCsvSatiri.cs         # Parsed and validated CSV record model
    │   ├── KioskListeModeli.cs       # Kiosk directory filters and pagination model
    │   └── LoginViewModel.cs         # User authentication form model
    │
    ├── Services/                     # Business Logic and Database Services
    │   ├── CabinextApiGirisServisi.cs# External OAuth token client service
    │   ├── DashboardService.cs       # Metrics aggregation and audit log retention service
    │   ├── KioskCsvOkuyucu.cs        # TextFieldParser-based RFC-compliant CSV reader
    │   ├── KioskDogrulamaServisi.cs  # Hierarchy and business validation rules engine
    │   ├── KioskKayitServisi.cs      # Transactional batch write and logging engine
    │   └── Localization.cs           # Multi-language string resource dictionary (TR/EN/AZ/RU)
    │
    ├── Views/                        # Razor Views (.cshtml)
    │   ├── Auth/                     # Login page
    │   ├── Home/                     # Dashboard view
    │   ├── Kiosk/                    # CSV Upload, Result, Directory, and Detail views
    │   └── Shared/                   # Layout template (_Layout) and error handlers
    │
    ├── Global.asax.cs                # Application bootstrap and per-request culture binding
    ├── Web.config                    # Active local environment configuration
    ├── Web.config.example            # Sanitized configuration template for deployment
    └── packages.config               # NuGet dependency manifest
```

---

## 🗄️ Database Models & Schema

The application operates across two dedicated MySQL databases:
1. `cabinext`: Stores master kiosk devices, movement transactions, locations, and organizational hierarchies.
2. `cabinext_logs`: Stores rejected rows and validation exceptions for auditability.

### 1. Primary Operational Database (`cabinext`)

```sql
-- Switch to primary database
CREATE DATABASE IF NOT EXISTS cabinext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE cabinext;

-- 1. Locations table (Distributors, Customers, Warehouses)
CREATE TABLE IF NOT EXISTS locations (
    Code VARCHAR(8) NOT NULL PRIMARY KEY,              -- Location code (e.g., DIST01, CUST101)
    Name VARCHAR(150) NULL,                            -- Location commercial name
    TypeCode INT NOT NULL,                             -- 1: Distributor, 2: Warehouse, 5: Customer
    Status INT NOT NULL DEFAULT 1,                     -- 1: Active, 0: Inactive
    CreatedOn DATETIME DEFAULT CURRENT_TIMESTAMP       -- Record creation timestamp
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. Location Hierarchies table (Parent-Child relations)
CREATE TABLE IF NOT EXISTS locationhierarchies (
    Id INT AUTO_INCREMENT PRIMARY KEY,                 -- Primary identity
    ParentPlaceCode VARCHAR(8) NOT NULL,               -- Parent code (Warehouse or Distributor)
    ChildPlaceCode VARCHAR(8) NOT NULL,                -- Child code (Distributor or Customer)
    ParentPlaceType INT NOT NULL,                      -- 1: Distributor, 2: Warehouse
    Status INT NOT NULL DEFAULT 1,                     -- 1: Active relationship, 0: Inactive
    INDEX idx_hierarchy_lookup (ChildPlaceCode, ParentPlaceType, Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 3. Cabinets table (Main Kiosk registry)
CREATE TABLE IF NOT EXISTS cabinets (
    Code VARCHAR(13) NOT NULL PRIMARY KEY,             -- Kiosk code (Mapped from KioskD in CSV)
    Barcode VARCHAR(13) NOT NULL UNIQUE,               -- Physical barcode (Mapped from KioskD)
    Year INT NOT NULL,                                 -- Commissioning year
    TypeCode VARCHAR(10) DEFAULT '140',                -- Device type category
    FAssetNo VARCHAR(13) NOT NULL,                     -- Fixed asset tracking number
    Status VARCHAR(10) DEFAULT '01',                   -- Internal status code
    CabinetView INT DEFAULT 3,                         -- Display category index
    LastAction VARCHAR(10) DEFAULT '22',               -- Last movement action (22: Customer delivery)
    Active TINYINT(1) DEFAULT 1,                       -- 1: Active, 0: Passive
    InDate DATETIME NOT NULL,                          -- Registration date
    Owner INT DEFAULT 0,                               -- Ownership identifier
    RLocCode VARCHAR(8) NOT NULL,                      -- Responsible distributor code
    RCustCode VARCHAR(8) NOT NULL,                     -- Responsible customer code
    WhCode VARCHAR(8) NOT NULL,                        -- Linked warehouse code
    CreatedBy INT DEFAULT 1,                           -- System user identity
    CreatedOn DATETIME DEFAULT CURRENT_TIMESTAMP,      -- Creation timestamp
    UsageStatus INT DEFAULT 0,                         -- Operational usage flag
    Reachable TINYINT(1) DEFAULT 1,                    -- Connectivity health (1: Online, 0: Unreachable)
    SubTypeCode VARCHAR(10) DEFAULT '239',             -- Device subtype
    SenderCode VARCHAR(10) DEFAULT 'HO',               -- Source dispatch code (HO: Head Office)
    NfcStatus INT DEFAULT 0,                           -- NFC peripheral flag
    BrandCode VARCHAR(20) DEFAULT 'MGM',               -- Hardware brand identifier
    INDEX idx_cabinets_lookup (Barcode, RLocCode, RCustCode, WhCode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 4. Cabinet Movements table (Logistics and custody movements)
CREATE TABLE IF NOT EXISTS cabinetmovements (
    Id INT AUTO_INCREMENT PRIMARY KEY,                 -- Auto-increment tracking id
    Code VARCHAR(13) NOT NULL,                         -- Kiosk code reference
    Barcode VARCHAR(13) NOT NULL,                      -- Physical barcode reference
    LineNumber INT NOT NULL,                           -- Movement sequence order (1, 2)
    MovementType VARCHAR(10) NOT NULL,                 -- Movement code: '32' (Initial WH) or '22' (Customer)
    Sender VARCHAR(20) NOT NULL,                       -- Dispatching party ('HO' or Warehouse code)
    Receiver VARCHAR(20) NOT NULL,                     -- Receiving party (Warehouse code or Customer code)
    RLocCode VARCHAR(8) NULL,                          -- Related distributor code
    Date DATETIME DEFAULT CURRENT_TIMESTAMP,           -- Movement recorded date
    Description VARCHAR(255) NULL,                     -- Movement remark
    CreatedBy INT DEFAULT 1,                           -- User who triggered movement
    CreatedOn DATETIME DEFAULT CURRENT_TIMESTAMP,      -- Creation timestamp
    INDEX idx_movement_code (Code, LineNumber)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### 2. Audit Log Database (`cabinext_logs`)

```sql
-- Switch to audit log database
CREATE DATABASE IF NOT EXISTS cabinext_logs CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE cabinext_logs;

-- Failed Kiosks audit table
CREATE TABLE IF NOT EXISTS failed_kiosks (
    Id INT AUTO_INCREMENT PRIMARY KEY,                 -- Log primary identity
    KioskCode VARCHAR(50) NULL,                        -- Target kiosk barcode/code
    Barcode VARCHAR(50) NULL,                          -- Scanned barcode
    DistributorCode VARCHAR(20) NULL,                  -- Provided distributor code
    CustomerCode VARCHAR(20) NULL,                     -- Provided customer code
    WarehouseCode VARCHAR(20) NULL,                    -- Resolved warehouse code (if any)
    ErrorMessage TEXT NOT NULL,                        -- Detailed validation or database error reason
    FailedAt DATETIME DEFAULT CURRENT_TIMESTAMP,       -- Failure timestamp (Used for 30-day retention cleanup)
    INDEX idx_failed_lookup (KioskCode, FailedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## 📄 Expected CSV File Specification

The uploaded `.csv` file MUST include the exact five column headers in the first row:

```csv
Dkodu,MKodu,KabinD,KioskD,CustCode
```

### Column Validation Matrix

| Column Header | Data Type | Max Length | Mandatory? | Business Rules & Constraints |
| :--- | :--- | :--- | :--- | :--- |
| **`Dkodu`** | String | 8 Chars | **Yes** | Distributor location code. Must exist in `locations` where `TypeCode = 1` and `Status = 1`. |
| **`MKodu`** | String | - | No | Hardware / Model code. |
| **`KabinD`** | String | - | No | Cabinet type / description identifier. |
| **`KioskD`** | String | 13 Chars | **Yes** | Kiosk physical barcode and code. Must be unique both within the CSV and against `cabinets.Barcode`. |
| **`CustCode`**| String | 8 Chars | **Yes** | Customer location code. Must exist in `locations` where `TypeCode = 5` and `Status = 1`, and must be actively mapped to `Dkodu` in `locationhierarchies`. |

### Sample CSV File Content (`sample_kiosks.csv`)

```csv
Dkodu,MKodu,KabinD,KioskD,CustCode
DST001,MOD-A,KAB-01,987654321012,CST00101
DST001,MOD-A,KAB-02,987654321013,CST00102
DST002,MOD-B,KAB-01,987654321014,CST00201
```

---

## ⚙️ Installation & Setup Guide

### 1. Prerequisites
* **Operating System:** Windows 10 / 11 or Windows Server 2016+
* **IDE:** [Visual Studio 2022](https://visualstudio.microsoft.com/) (with *.NET desktop development* and *ASP.NET and web development* workloads)
* **SDK:** .NET Framework 4.7.2 Developer Pack
* **Database Engine:** MySQL Server 8.0+ (or MariaDB / XAMPP / Docker container)
* **Web Server:** IIS Express (Included in Visual Studio) or Full IIS

### 2. Clone the Repository

```bash
# Clone the repository to your local workspace
git clone https://github.com/your-organization/CabinextKioskAktarim.git

# Navigate into the solution root folder
cd CabinextKioskAktarim
```

### 3. Database Provisioning
Open your database management tool (MySQL Workbench, phpMyAdmin, HeidiSQL, or CLI) and execute the SQL scripts above to create both the `cabinext` and `cabinext_logs` databases.

You can populate sample test hierarchy data using the following script:

```sql
-- Sample Seed Data for testing
USE cabinext;

-- Insert sample distributor, warehouse, and customer
INSERT INTO locations (Code, Name, TypeCode, Status) VALUES
('WH01',   'Central Logistics Warehouse', 2, 1), -- Warehouse
('DST001', 'Marmara Distributor',        1, 1), -- Distributor
('CST00101','Kadikoy Test Customer',     5, 1); -- Customer

-- Link Distributor to Warehouse (ParentPlaceType = 2)
INSERT INTO locationhierarchies (ParentPlaceCode, ChildPlaceCode, ParentPlaceType, Status) VALUES
('WH01', 'DST001', 2, 1);

-- Link Customer to Distributor (ParentPlaceType = 1)
INSERT INTO locationhierarchies (ParentPlaceCode, ChildPlaceCode, ParentPlaceType, Status) VALUES
('DST001', 'CST00101', 1, 1);
```

### 4. Configuration (`Web.config` Settings)
You can copy [`CabinextKioskAktarim/Web.config.example`](file:///c:/Users/Casper/Desktop/cabinext/CabinextKioskAktarim/CabinextKioskAktarim/Web.config.example) to create your local `Web.config`:

```xml
<configuration>
  <connectionStrings>
    <!-- Primary operational database connection string -->
    <add name="CabinextLocal"
         connectionString="Server=127.0.0.1;Port=3306;Database=cabinext;Uid=YOUR_DB_USER;Pwd=YOUR_DB_PASSWORD;"
         providerName="MySql.Data.MySqlClient" />

    <!-- Error logging and audit database connection string -->
    <add name="CabinextLogs"
         connectionString="Server=127.0.0.1;Port=3306;Database=cabinext_logs;Uid=YOUR_DB_USER;Pwd=YOUR_DB_PASSWORD;"
         providerName="MySql.Data.MySqlClient" />
  </connectionStrings>

  <appSettings>
    <!-- Administrative Credentials (Configure securely) -->
    <add key="AdminUsername" value="admin" />
    <add key="AdminPassword" value="YOUR_SECURE_PASSWORD" />

    <!-- External Cabinext OAuth API Endpoints -->
    <add key="CabinextApiBaseUrl" value="https://api.yourdomain.com/" />
    <add key="CabinextTokenUrl" value="https://api.yourdomain.com/api/token" />
    <add key="CabinextClientId" value="YOUR_CLIENT_ID" />
  </appSettings>
  ...
</configuration>
```

### 5. Restore Dependencies & Build Solution
Open the solution in Visual Studio:
1. Double-click `CabinextKioskAktarim.sln` to open it in Visual Studio 2022.
2. Select **Tools &rarr; NuGet Package Manager &rarr; Restore NuGet Packages** from the top menu.
3. Alternatively, run the following command in the **Package Manager Console**:

```powershell
# Restore and reinstall packages defined in packages.config
Update-Package -Reinstall -ProjectName CabinextKioskAktarim
```

4. Build the solution: Press `Ctrl + Shift + B` (or select **Build &rarr; Build Solution**).

### 6. Run the Application
* Press `F5` (Start Debugging) or `Ctrl + F5` (Start Without Debugging) to launch the app on IIS Express.
* Your default web browser will automatically open the login page at `https://localhost:44359/Auth/Login`.

---

## 🔐 Authentication & Access Control

Direct access to system views is restricted by the `[CabinextOturum]` action filter.

* **Administrative Credentials:** Configured via `AdminUsername` and `AdminPassword` in `Web.config`.
* **Local Development Defaults (if unset in `Web.config`):**
  * **Username:** `admin`
  * **Password:** `Admin123!`
* **Production Recommendation:** Always specify a strong, unique password in `Web.config` before deploying to staging or production environments.

Upon successful authentication, a Forms Authentication ticket is issued, and a secure session (`Session["CabinextAccessToken"]`) is established.

---

## 🌐 Internationalization (i18n)

The application utilizes an in-memory dictionary provider ([Localization.cs](file:///c:/Users/Casper/Desktop/cabinext/CabinextKioskAktarim/CabinextKioskAktarim/Services/Localization.cs)), avoiding compilation dependencies on `.resx` files.

### Supported Cultures
* 🇹🇷 **Turkish:** `tr-TR` (Default)
* 🇬🇧 **English:** `en-US`
* 🇦🇿 **Azerbaijani:** `az-AZ`
* 🇷🇺 **Russian:** `ru-RU`

### Usage in Razor Views

```csharp
@using CabinextKioskAktarim.Services

<!-- Fetch localized string based on active thread culture -->
<h1 class="page-title">@Localization.Get("Dashboard")</h1>
<p class="text-muted">@Localization.Get("DashboardDesc")</p>
```

### Adding New Keys or Languages

Add the new culture code or key-value pair to the `Resources` dictionary in `Localization.cs`:

```csharp
// Example: Adding new localization key across supported cultures
Resources["tr-TR"]["NewKey"] = "Yeni Başlık";
Resources["en-US"]["NewKey"] = "New Title";
Resources["az-AZ"]["NewKey"] = "Yeni Başlıq";
Resources["ru-RU"]["NewKey"] = "Новый Заголовок";
```

---

## 🔍 Troubleshooting & FAQ

### 1. `Unable to connect to any of the specified MySQL hosts.`
* **Cause:** MySQL service is inactive or network credentials/port are wrong.
* **Solution:** Verify the MySQL service is running in Windows Services (`services.msc`). Double-check the host IP (`127.0.0.1`), `Port` (default: 3306), `Uid`, and `Pwd` in `Web.config`.

### 2. `The anti-forgery token could not be decrypted.` or `Validation failed.`
* **Cause:** Stale authentication cookies lingering from previous sessions or mismatching request tokens.
* **Solution:** Clear browser cookies or launch a private/incognito session. The sign-out flow in `AuthController.cs` is designed to clear stale cookies automatically.

### 3. `CSV headers are missing or in an unexpected order.`
* **Cause:** The CSV file headers do not strictly match `Dkodu,MKodu,KabinD,KioskD,CustCode`, or the file is delimited by semicolons (`;`) instead of commas.
* **Solution:** Save the spreadsheet explicitly as a comma-delimited `.csv` with UTF-8 encoding.

### 4. `Customer CST00101 is linked to distributor DST001; CSV specifies DST002.`
* **Cause:** The hierarchy validation detected that the customer belongs to a different distributor.
* **Solution:** Review the mapping in the `locationhierarchies` table, or correct the `Dkodu` value in your CSV.

---

## 📄 License & Maintenance

© 2026 **Cabinext Information Technologies Inc.** All rights reserved.  
This software is proprietary. Unauthorized copying, modification, or distribution is strictly prohibited.
