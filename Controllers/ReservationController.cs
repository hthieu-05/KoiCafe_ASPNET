using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Configuration;

namespace KoiCafe.Controllers
{
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

    public class BanInfo 
    {
        public int MaBan { get; set; }
        public string TenBan { get; set; }
        public int SucChua { get; set; }
        public string TrangThai { get; set; }
    }

    public class ReservationController : Controller
    {
        private readonly string _conn;
        public ReservationController(IConfiguration config) { _conn = config.GetConnectionString("DefaultConnection"); }

        [HttpGet]
        public IActionResult Index()
        {
            var danhSach = new List<DatBanViewModel>(); 
            var danhSachBan = new List<BanInfo>();

            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                string sqlDatBan = "SELECT * FROM DatBan ORDER BY ThoiGianDat DESC";
                using (SqlCommand cmd = new SqlCommand(sqlDatBan, conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        danhSach.Add(new DatBanViewModel {
                            MaDatBan = Convert.ToInt32(r["MaDatBan"]),
                            TenKhach = r["TenKhach"] != DBNull.Value ? r["TenKhach"].ToString() : "Khách",
                            SoDienThoai = r["SoDienThoai"] != DBNull.Value ? r["SoDienThoai"].ToString() : "",
                            SoNguoi = r["SoNguoi"] != DBNull.Value ? Convert.ToInt32(r["SoNguoi"]) : 1,
                            ThoiGianDat = r["ThoiGianDat"] != DBNull.Value ? Convert.ToDateTime(r["ThoiGianDat"]) : DateTime.Now,
                            DanhSachMaBan = r["DanhSachMaBan"] != DBNull.Value ? r["DanhSachMaBan"].ToString() : "",
                            TrangThai = r["TrangThai"] != DBNull.Value ? r["TrangThai"].ToString() : "Chờ xác nhận"
                        });
                    }
                }

                string sqlBan = "SELECT MaBan, TenBan, SucChua, TrangThai FROM Ban ORDER BY TrangThai DESC, TenBan ASC";
                using (SqlCommand cmdBan = new SqlCommand(sqlBan, conn))
                using (SqlDataReader rBan = cmdBan.ExecuteReader())
                {
                    while (rBan.Read())
                    {
                        danhSachBan.Add(new BanInfo {
                            MaBan = Convert.ToInt32(rBan["MaBan"]),
                            TenBan = rBan["TenBan"].ToString(),
                            SucChua = rBan["SucChua"] != DBNull.Value ? Convert.ToInt32(rBan["SucChua"]) : 4,
                            TrangThai = rBan["TrangThai"].ToString()
                        });
                    }
                }
            }

            ViewBag.DanhSachBan = danhSachBan; 
            return View(danhSach);
        }

        [HttpPost]
        public IActionResult DuyetDonThuCong(int id, string chuoiBan)
        {
            if (string.IsNullOrEmpty(chuoiBan)) 
                return Json(new { success = false, message = "Vui lòng chọn ít nhất 1 bàn!" });

            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    string sql = $"UPDATE DatBan SET TrangThai = N'Đã xác nhận', DanhSachMaBan = '{chuoiBan}' WHERE MaDatBan = {id}";
                    new SqlCommand(sql, conn, trans).ExecuteNonQuery();

                    string sqlBan = $"UPDATE Ban SET TrangThai = N'Đã đặt' WHERE MaBan IN ({chuoiBan})";
                    new SqlCommand(sqlBan, conn, trans).ExecuteNonQuery();

                    trans.Commit();
                    return Json(new { success = true, message = $"Đã xếp bàn thủ công thành công!" });
                }
                catch(Exception ex)
                {
                    trans.Rollback();
                    return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
                }
            }
        }

        [HttpPost]
        public IActionResult XacNhanKhachDen(int maDatBan, string hanhDong)
        {
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                var dsBan = new SqlCommand($"SELECT DanhSachMaBan FROM DatBan WHERE MaDatBan = {maDatBan}", conn).ExecuteScalar()?.ToString();

                if (hanhDong == "DaDen")
                {
                    new SqlCommand($"UPDATE DatBan SET TrangThai = N'Đã đến' WHERE MaDatBan = {maDatBan}", conn).ExecuteNonQuery();
                    if (!string.IsNullOrEmpty(dsBan))
                    {
                        new SqlCommand($"UPDATE Ban SET TrangThai = N'Đang phục vụ' WHERE MaBan IN ({dsBan})", conn).ExecuteNonQuery();
                    }
                }
                else if (hanhDong == "KhongDen") 
                {
                    new SqlCommand($"UPDATE DatBan SET TrangThai = N'Không đến' WHERE MaDatBan = {maDatBan}", conn).ExecuteNonQuery();
                    if (!string.IsNullOrEmpty(dsBan))
                    {
                        new SqlCommand($"UPDATE Ban SET TrangThai = N'Trong' WHERE MaBan IN ({dsBan})", conn).ExecuteNonQuery();
                    }
                }
            }
            return RedirectToAction("Index"); 
        }
    }
}