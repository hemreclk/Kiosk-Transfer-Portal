using System;
using System.Collections.Generic;

namespace CabinextKioskAktarim.Models
{
    public class DashboardViewModel
    {
        public int TotalKiosks { get; set; }
        public int ActiveKiosks { get; set; }
        public int PassiveKiosks { get; set; }
        public int DeliveredKiosks { get; set; }
        public int UnreachableKiosks { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalWarehouses { get; set; }

        public List<KioskSummary> RecentKiosks { get; set; }
        public List<FailedKioskSummary> AttentionRequiredKiosks { get; set; }
        
        public string ErrorMessage { get; set; }

        public DashboardViewModel()
        {
            RecentKiosks = new List<KioskSummary>();
            AttentionRequiredKiosks = new List<FailedKioskSummary>();
        }
    }

    public class FailedKioskSummary
    {
        public string KioskCode { get; set; }
        public string DistributorCode { get; set; }
        public string CustomerCode { get; set; }
        public string WarehouseCode { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime? FailedAt { get; set; }
    }

    public class KioskSummary
    {
        public string Code { get; set; }
        public string Barcode { get; set; }
        public string RLocCode { get; set; }
        public string RCustCode { get; set; }
        public string WhCode { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? Active { get; set; } 
    }
}
