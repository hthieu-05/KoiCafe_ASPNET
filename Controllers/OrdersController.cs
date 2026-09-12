using KoiCafe.Filters;
using KoiCafe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KoiCafe.Controllers
{
    public class InvoiceProcessRequest
    {
        public int MaHD { get; set; }
        public int MaBan { get; set; }
        public decimal SoTienKhuyenMai { get; set; }
        public string HanhDong { get; set; } // "ThanhToan", "Huy", "LuuTam"
        public string LyDoHuy { get; set; } 
    }
    [AdminAuth] 
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

        // [GET] API: Lấy chi tiết các món ăn trong 1 hóa đơn
        [HttpGet]
        public IActionResult GetOrderDetails(int id)
        {
            var details = new List<ChiTietHoaDonModel>();
            try
            {
                using (SqlConnection conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    string sql = @"SELECT sp.TenSP, ct.SoLuong, ct.DonGia 
                                   FROM ChiTietHoaDon ct
                                   INNER JOIN SanPham sp ON ct.MaSP = sp.MaSP
                                   WHERE ct.MaHD = @MaHD";
                                   
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaHD", id);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                details.Add(new ChiTietHoaDonModel
                                {
                                    TenSP = r["TenSP"].ToString(),
                                    SoLuong = Convert.ToInt32(r["SoLuong"]),
                                    DonGia = Convert.ToDecimal(r["DonGia"])
                                });
                            }
                        }
                    }
                }
                return Json(new { success = true, data = details });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}