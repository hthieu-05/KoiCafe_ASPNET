using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System;

namespace KoiCafe.Controllers
{
    // 1. Khuôn dữ liệu riêng cho trang Quản lý đặt bàn (Không đụng hàng với code cũ)
    public class DatBanViewModel
    {
        public int MaDatBan { get; set; }
        public string TenKhach { get; set; }
        public string SoDienThoai { get; set; }
        public int SoNguoi { get; set; }
        public DateTime ThoiGianDat { get; set; }
        public string DanhSachMaBan { get; set; }
        public string TrangThai { get; set; }
    }

    public class BanTamThuatToan 
    {
        public int MaBan { get; set; }
        public int SucChua { get; set; }
    }

    public class ReservationController : Controller
    {
        private readonly string _conn;
        public ReservationController(IConfiguration config) { _conn = config.GetConnectionString("DefaultConnection"); }

        [HttpGet]
        public IActionResult Index()
        {
            var danhSach = new List<DatBanViewModel>(); 
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                string sql = "SELECT * FROM DatBan ORDER BY ThoiGianDat DESC";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        danhSach.Add(new DatBanViewModel {
                            MaDatBan = Convert.ToInt32(r["MaDatBan"]),
                            // Dùng GetValue để phòng hờ DB của bạn dùng tên cột khác hoặc bị Null
                            TenKhach = r["TenKhach"] != DBNull.Value ? r["TenKhach"].ToString() : "Khách",
                            SoDienThoai = r["SoDienThoai"] != DBNull.Value ? r["SoDienThoai"].ToString() : "",
                            SoNguoi = r["SoNguoi"] != DBNull.Value ? Convert.ToInt32(r["SoNguoi"]) : 1,
                            ThoiGianDat = r["ThoiGianDat"] != DBNull.Value ? Convert.ToDateTime(r["ThoiGianDat"]) : DateTime.Now,
                            DanhSachMaBan = r["DanhSachMaBan"] != DBNull.Value ? r["DanhSachMaBan"].ToString() : "",
                            TrangThai = r["TrangThai"] != DBNull.Value ? r["TrangThai"].ToString() : "Chờ xác nhận"
                        });
                    }
                }
            }
            return View(danhSach);
        }

        private List<int> TimBanPhuHop(int soNguoi, SqlConnection conn)
        {
            List<BanTamThuatToan> banTrong = new List<BanTamThuatToan>();
            using (SqlCommand cmd = new SqlCommand("SELECT MaBan, SucChua FROM Ban WHERE TrangThai = N'Trong' ORDER BY SucChua ASC", conn))
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                while (r.Read()) banTrong.Add(new BanTamThuatToan { MaBan = Convert.ToInt32(r["MaBan"]), SucChua = Convert.ToInt32(r["SucChua"]) });
            }

            var ban2 = banTrong.Where(b => b.SucChua == 2).ToList();
            var ban4 = banTrong.Where(b => b.SucChua == 4).ToList();
            var ban6 = banTrong.Where(b => b.SucChua == 6).ToList();
            List<int> kq = new List<int>();

            if (soNguoi <= 2) { if (ban2.Count > 0) kq.Add(ban2[0].MaBan); else if (ban4.Count > 0) kq.Add(ban4[0].MaBan); }
            else if (soNguoi <= 4) { if (ban4.Count > 0) kq.Add(ban4[0].MaBan); else if (ban6.Count > 0) kq.Add(ban6[0].MaBan); }
            else if (soNguoi <= 6) { if (ban6.Count > 0) kq.Add(ban6[0].MaBan); else if (ban4.Count >= 2) { kq.Add(ban4[0].MaBan); kq.Add(ban4[1].MaBan); } }
            else if (soNguoi <= 8) { if (ban4.Count >= 2) { kq.Add(ban4[0].MaBan); kq.Add(ban4[1].MaBan); } }
            else if (soNguoi <= 10) { if (ban4.Count > 0 && ban6.Count > 0) { kq.Add(ban4[0].MaBan); kq.Add(ban6[0].MaBan); } }
            return kq; 
        }

        [HttpPost]
        public IActionResult DuyetDon(int id, int soNguoi)
        {
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                var dsBan = TimBanPhuHop(soNguoi, conn);
                if (dsBan.Count == 0) return Json(new { success = false, message = "Hiện tại quán không đủ bàn trống để xếp cho số lượng người này!" });

                string chuoiBan = string.Join(",", dsBan);
                string sql = $"UPDATE DatBan SET TrangThai = N'Đã xác nhận', DanhSachMaBan = '{chuoiBan}' WHERE MaDatBan = {id}";
                new SqlCommand(sql, conn).ExecuteNonQuery();
                return Json(new { success = true, message = $"Đã tự động xếp bàn số: {chuoiBan}" });
            }
        }

        [HttpPost]
        public IActionResult XacNhanKhachDen(int maDatBan, string hanhDong)
        {
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                if (hanhDong == "DaDen")
                {
                    new SqlCommand($"UPDATE DatBan SET TrangThai = N'Đã đến' WHERE MaDatBan = {maDatBan}", conn).ExecuteNonQuery();
                    var dsBan = new SqlCommand($"SELECT DanhSachMaBan FROM DatBan WHERE MaDatBan = {maDatBan}", conn).ExecuteScalar()?.ToString();
                    if (!string.IsNullOrEmpty(dsBan))
                    {
                        new SqlCommand($"UPDATE Ban SET TrangThai = N'Đang phục vụ' WHERE MaBan IN ({dsBan})", conn).ExecuteNonQuery();
                    }
                }
            }
            return RedirectToAction("Index"); 
        }
    }
}