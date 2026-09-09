using KoiCafe.Filters;
using KoiCafe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KoiCafe.Controllers
{
    [AdminAuth] // Chặn chỉ cho Admin truy cập
    public class OrdersController : Controller
    {
        private readonly string _conn;
        public OrdersController(IConfiguration config) { _conn = config.GetConnectionString("DefaultConnection"); }

        [HttpGet]
        public IActionResult Index()
        {
            var invoices = new List<HoaDonModel>();

            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                // Kéo dữ liệu từ 3 bảng: HoaDon, Ban, NhanVien
                string sql = @"SELECT hd.MaHD, b.TenBan, nv.TenNV, hd.NgayLap, hd.TongTien, hd.TrangThai, hd.LyDoHuy 
                               FROM HoaDon hd 
                               LEFT JOIN Ban b ON hd.MaBan = b.MaBan 
                               LEFT JOIN NhanVien nv ON hd.MaNV = nv.MaNV 
                               ORDER BY hd.NgayLap DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            invoices.Add(new HoaDonModel
                            {
                                MaHD = Convert.ToInt32(reader["MaHD"]),
                                TenBan = reader["TenBan"] != DBNull.Value ? reader["TenBan"].ToString() : "Trống",
                                TenNV = reader["TenNV"] != DBNull.Value ? reader["TenNV"].ToString() : "Hệ thống",
                                NgayLap = Convert.ToDateTime(reader["NgayLap"]),
                                TongTien = Convert.ToDecimal(reader["TongTien"]),
                                TrangThai = reader["TrangThai"].ToString(),
                                LyDoHuy = reader["LyDoHuy"] != DBNull.Value ? reader["LyDoHuy"].ToString() : ""
                            });
                        }
                    }
                }
            }
            return View(invoices);
        }
    }
}