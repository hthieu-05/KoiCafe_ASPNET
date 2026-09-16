using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System;

namespace KoiCafe.Controllers
{
    public class KhachHangModel
    {
        public int MaKH { get; set; }
        public string HoTen { get; set; }
        public string SDT { get; set; } // Giữ tên biến là SDT để View không bị lỗi
        public int DiemTichLuy { get; set; }
        public DateTime NgayDangKy { get; set; }
    }

    public class CustomerController : Controller
    {
        private readonly string _conn;
        public CustomerController(IConfiguration config) { _conn = config.GetConnectionString("DefaultConnection"); }

        [HttpGet]
        public IActionResult Index(string searchQuery = "")
        {
            var dsKhach = new List<KhachHangModel>();
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                // Sửa thành SoDienThoai
                string sql = @"SELECT * FROM KhachHang 
                               WHERE HoTen LIKE @Search OR SoDienThoai LIKE @Search 
                               ORDER BY NgayDangKy DESC";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Search", "%" + searchQuery + "%");
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            dsKhach.Add(new KhachHangModel
                            {
                                MaKH = Convert.ToInt32(r["MaKH"]),
                                HoTen = r["HoTen"].ToString(),
                                SDT = r["SoDienThoai"].ToString(), // Lấy từ cột SoDienThoai
                                DiemTichLuy = r["DiemTichLuy"] != DBNull.Value ? Convert.ToInt32(r["DiemTichLuy"]) : 0,
                                NgayDangKy = r["NgayDangKy"] != DBNull.Value ? Convert.ToDateTime(r["NgayDangKy"]) : DateTime.Now
                            });
                        }
                    }
                }
            }
            ViewBag.SearchQuery = searchQuery;
            return View(dsKhach);
        }

        [HttpPost]
        public IActionResult AddCustomer(string hoTen, string sdt)
        {
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                var checkCmd = new SqlCommand("SELECT COUNT(1) FROM KhachHang WHERE SoDienThoai = @SDT", conn);
                checkCmd.Parameters.AddWithValue("@SDT", sdt);
                if ((int)checkCmd.ExecuteScalar() > 0)
                {
                    TempData["Error"] = "Số điện thoại này đã được đăng ký!";
                    return RedirectToAction("Index");
                }

                // CẬP NHẬT: Thêm cột MatKhau và cấp sẵn mật khẩu mặc định '123456' để vượt qua lỗi Database
                string sql = "INSERT INTO KhachHang (HoTen, SoDienThoai, DiemTichLuy, NgayDangKy, MatKhau) VALUES (@Ten, @SDT, 0, GETDATE(), '123456')";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Ten", hoTen);
                    cmd.Parameters.AddWithValue("@SDT", sdt);
                    cmd.ExecuteNonQuery();
                }
                TempData["Success"] = "Thêm khách hàng thành công!";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult CheckPhone(string sdt)
        {
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                // Sửa thành SoDienThoai
                string sql = "SELECT MaKH, HoTen, DiemTichLuy FROM KhachHang WHERE SoDienThoai = @SDT";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@SDT", sdt);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            return Json(new { 
                                success = true, 
                                data = new { 
                                    MaKH = r["MaKH"], 
                                    HoTen = r["HoTen"].ToString(), 
                                    DiemTichLuy = r["DiemTichLuy"] 
                                } 
                            });
                        }
                    }
                }
            }
            return Json(new { success = false, message = "Chưa có khách hàng này." });
        }
    }
}