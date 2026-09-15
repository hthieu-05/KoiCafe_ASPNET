using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System;

namespace KoiCafe.Controllers
{
    public class TableViewModel
    {
        public int MaBan { get; set; }
        public string TenBan { get; set; }
        public string KhuVuc { get; set; }
        public int SucChua { get; set; }
        public string TrangThai { get; set; }
    }

    public class TablesController : Controller
    {
        private readonly string _conn;
        public TablesController(IConfiguration config) { _conn = config.GetConnectionString("DefaultConnection"); }

        [HttpGet]
        public IActionResult Index()
        {
            var dsBan = new List<TableViewModel>();
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM Ban ORDER BY KhuVuc, TenBan", conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        dsBan.Add(new TableViewModel
                        {
                            MaBan = Convert.ToInt32(r["MaBan"]),
                            TenBan = r["TenBan"].ToString(),
                            KhuVuc = r["KhuVuc"] != DBNull.Value ? r["KhuVuc"].ToString() : "Sảnh chính",
                            SucChua = r["SucChua"] != DBNull.Value ? Convert.ToInt32(r["SucChua"]) : 4,
                            TrangThai = r["TrangThai"].ToString()
                        });
                    }
                }
            }
            return View(dsBan);
        }

        [HttpPost]
        public IActionResult AddTable(string tenBan, string khuVuc, int sucChua)
        {
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                string sql = "INSERT INTO Ban (TenBan, KhuVuc, SucChua, TrangThai) VALUES (@Ten, @KV, @SC, N'Trong')";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Ten", tenBan);
                    cmd.Parameters.AddWithValue("@KV", khuVuc);
                    cmd.Parameters.AddWithValue("@SC", sucChua);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }

        // --- CHỨC NĂNG SỬA BÀN MỚI THÊM ---
        [HttpPost]
        public IActionResult EditTable(int maBan, string tenBan, string khuVuc, int sucChua)
        {
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                string sql = "UPDATE Ban SET TenBan = @Ten, KhuVuc = @KV, SucChua = @SC WHERE MaBan = @MaBan";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Ten", tenBan);
                    cmd.Parameters.AddWithValue("@KV", khuVuc);
                    cmd.Parameters.AddWithValue("@SC", sucChua);
                    cmd.Parameters.AddWithValue("@MaBan", maBan);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteTable(int id)
        {
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                var tt = new SqlCommand($"SELECT TrangThai FROM Ban WHERE MaBan = {id}", conn).ExecuteScalar()?.ToString();
                if (tt != "Trong") return Json(new { success = false, message = "Không thể xóa bàn đang có khách!" });

                new SqlCommand($"DELETE FROM Ban WHERE MaBan = {id}", conn).ExecuteNonQuery();
                return Json(new { success = true });
            }
        }

        [HttpPost]
        public IActionResult MoveTable(int banCu, int banMoi)
        {
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    string sqlMoveHD = "UPDATE HoaDon SET MaBan = @Moi WHERE MaBan = @Cu AND TrangThai = N'Chưa thanh toán'";
                    using (SqlCommand cmd = new SqlCommand(sqlMoveHD, conn, trans))
                    {
                        cmd.Parameters.AddWithValue("@Moi", banMoi);
                        cmd.Parameters.AddWithValue("@Cu", banCu);
                        cmd.ExecuteNonQuery();
                    }

                    new SqlCommand($"UPDATE Ban SET TrangThai = N'Trong' WHERE MaBan = {banCu}", conn, trans).ExecuteNonQuery();
                    new SqlCommand($"UPDATE Ban SET TrangThai = N'Đang phục vụ' WHERE MaBan = {banMoi}", conn, trans).ExecuteNonQuery();

                    trans.Commit();
                    return Json(new { success = true, message = "Chuyển bàn thành công!" });
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }
            }
        }

        [HttpPost]
        public IActionResult MergeTable(int banNguon, int banDich)
        {
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                int hdNguon = Convert.ToInt32(new SqlCommand($"SELECT MaHD FROM HoaDon WHERE MaBan = {banNguon} AND TrangThai = N'Chưa thanh toán'", conn).ExecuteScalar() ?? 0);
                int hdDich = Convert.ToInt32(new SqlCommand($"SELECT MaHD FROM HoaDon WHERE MaBan = {banDich} AND TrangThai = N'Chưa thanh toán'", conn).ExecuteScalar() ?? 0);

                if (hdNguon == 0 || hdDich == 0) return Json(new { success = false, message = "Cả 2 bàn phải đang phục vụ mới gộp được!" });

                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    new SqlCommand($"UPDATE ChiTietHoaDon SET MaHD = {hdDich} WHERE MaHD = {hdNguon}", conn, trans).ExecuteNonQuery();
                    new SqlCommand($"UPDATE HoaDon SET TongTien = (SELECT ISNULL(SUM(ThanhTien), 0) FROM ChiTietHoaDon WHERE MaHD = {hdDich}) WHERE MaHD = {hdDich}", conn, trans).ExecuteNonQuery();
                    new SqlCommand($"UPDATE HoaDon SET TrangThai = N'Đã hủy', LyDoHuy = N'Gộp hóa đơn sang Bàn {banDich}' WHERE MaHD = {hdNguon}", conn, trans).ExecuteNonQuery();
                    new SqlCommand($"UPDATE Ban SET TrangThai = N'Trong' WHERE MaBan = {banNguon}", conn, trans).ExecuteNonQuery();

                    trans.Commit();
                    return Json(new { success = true, message = "Gộp bàn thành công!" });
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }
            }
        }
    }
}