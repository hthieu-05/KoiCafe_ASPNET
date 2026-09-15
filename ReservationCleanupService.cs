using Microsoft.Data.SqlClient;

namespace KoiCafe.Services 
{
    public class ReservationCleanupService : BackgroundService
    {
        private readonly IConfiguration _config;
        public ReservationCleanupService(IConfiguration config) { _config = config; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    string connStr = _config.GetConnectionString("DefaultConnection");
                    using (SqlConnection conn = new SqlConnection(connStr))
                    {
                        conn.Open();
                        string sql = @"
                            SELECT MaDatBan, DanhSachMaBan 
                            FROM DatBan 
                            WHERE TrangThai = N'Đã xác nhận' 
                            AND DATEDIFF(MINUTE, ThoiGianDat, GETDATE()) >= 15";
                            
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            var expiredReservations = new List<(int, string)>();
                            while (r.Read()) expiredReservations.Add((Convert.ToInt32(r["MaDatBan"]), r["DanhSachMaBan"].ToString()));
                            r.Close();

                            foreach (var res in expiredReservations)
                            {
                                new SqlCommand($"UPDATE DatBan SET TrangThai = N'Không đến' WHERE MaDatBan = {res.Item1}", conn).ExecuteNonQuery();
                                new SqlCommand($"UPDATE Ban SET TrangThai = N'Trong' WHERE MaBan IN ({res.Item2})", conn).ExecuteNonQuery();
                            }
                        }
                    }
                }
                catch { /* Bỏ qua lỗi kết nối tạm thời */ }

                await Task.Delay(60000, stoppingToken); 
            }
        }
    }
}