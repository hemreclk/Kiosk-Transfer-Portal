using System;
using System.Collections.Generic;
using System.Configuration;
using MySql.Data.MySqlClient;
using CabinextKioskAktarim.Models;

namespace CabinextKioskAktarim.Services
{
    public class DashboardService
    {
        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["CabinextLocal"].ConnectionString;
        }

        public DashboardViewModel GetDashboardData()
        {
            var model = new DashboardViewModel();
            try
            {
                using (var connection = new MySqlConnection(GetConnectionString()))
                {
                    connection.Open();

                    // 1. Summary Metrics (Successful Kiosks - cabinext database)
                    string queryCounts = @"
                        SELECT 
                            (SELECT COUNT(*) FROM cabinets) AS ActiveKiosks,
                            (SELECT COUNT(*) FROM cabinets WHERE LastAction = '22') AS DeliveredKiosks,
                            (SELECT COUNT(*) FROM cabinets WHERE Reachable = 0) AS UnreachableKiosks,
                            (SELECT COUNT(DISTINCT RCustCode) FROM cabinets WHERE RCustCode IS NOT NULL AND RCustCode != '') AS TotalCustomers,
                            (SELECT COUNT(DISTINCT WhCode) FROM cabinets WHERE WhCode IS NOT NULL AND WhCode != '') AS TotalWarehouses
                    ";
                    
                    using (var cmd = new MySqlCommand(queryCounts, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                model.ActiveKiosks = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader[0]);
                                model.DeliveredKiosks = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader[1]);
                                model.UnreachableKiosks = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader[2]);
                                model.TotalCustomers = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader[3]);
                                model.TotalWarehouses = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader[4]);
                            }
                        }
                    }

                    // 1.1 Summary Metrics (Failed Kiosks - cabinext_logs database)
                    try
                    {
                        using (var logConnection = new MySqlConnection(ConfigurationManager.ConnectionStrings["CabinextLogs"].ConnectionString))
                        {
                            logConnection.Open();
                            using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM failed_kiosks", logConnection))
                            {
                                model.PassiveKiosks = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                        }
                    }
                    catch (Exception)
                    {
                        model.PassiveKiosks = 0; // Fallback to 0 if log database is unavailable or table does not exist
                    }

                    // 1.2 Purge Historical Error Logs (Older than 30 days)
                    try
                    {
                        using (var logConnection = new MySqlConnection(ConfigurationManager.ConnectionStrings["CabinextLogs"].ConnectionString))
                        {
                            logConnection.Open();
                            // Delete records where FailedAt is older than 30 days
                            using (var cmd = new MySqlCommand("DELETE FROM failed_kiosks WHERE FailedAt < DATE_SUB(NOW(), INTERVAL 30 DAY)", logConnection))
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (Exception) { /* Suppress error if logging table or columns are not yet provisioned */ }

                    // Total Kiosks = Successful + Failed
                    model.TotalKiosks = model.ActiveKiosks + model.PassiveKiosks;


                    // 2. Recent 5 Kiosks
                    string queryRecent = @"
                        SELECT Code, Barcode, RLocCode, RCustCode, WhCode, CreatedOn, Active 
                        FROM cabinets 
                        ORDER BY CreatedOn DESC 
                        LIMIT 5
                    ";
                    using (var cmd = new MySqlCommand(queryRecent, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                model.RecentKiosks.Add(new KioskSummary
                                {
                                    Code = reader["Code"].ToString(),
                                    Barcode = reader["Barcode"].ToString(),
                                    RLocCode = reader["RLocCode"].ToString(),
                                    RCustCode = reader["RCustCode"].ToString(),
                                    WhCode = reader["WhCode"].ToString(),
                                    CreatedOn = reader["CreatedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["CreatedOn"]),
                                    Active = reader["Active"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Active"])
                                });
                            }
                        }
                    }

                    // 3. Dikkat Gerektiren Kayıtlar (Hatalı Kayıtlar - failed_kiosks)
                    try
                    {
                        using (var logConnection = new MySqlConnection(ConfigurationManager.ConnectionStrings["CabinextLogs"].ConnectionString))
                        {
                            logConnection.Open();
                            string queryAttention = @"
                                SELECT KioskCode, DistributorCode, CustomerCode, WarehouseCode, ErrorMessage 
                                FROM failed_kiosks 
                                LIMIT 5
                            ";
                            using (var cmd = new MySqlCommand(queryAttention, logConnection))
                            {
                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        model.AttentionRequiredKiosks.Add(new FailedKioskSummary
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
                        // Log veritabanından çekilemezse boş liste dönsün, hata patlamasın
                        model.ErrorMessage = "Hata (Dikkat Gerektiren Kayıtlar): " + ex.Message;
                    }
                }
            }
            catch (Exception)
            {
                model.ErrorMessage = "Veritabanı bağlantısı veya veri okuma sırasında bir hata oluştu. Lütfen bağlantı ayarlarını ve tabloları kontrol edin.";
            }
            
            return model;
        }
    }
}
