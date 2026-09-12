using KoiCafe.Filters;
using KoiCafe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KoiCafe.Controllers
{
    [AdminAuth] // Bảo vệ trang này, chỉ Admin mới được vào
    public class BookingsController : Controller
    {
        private readonly string _conn;
        public BookingsController(IConfiguration config) { _conn = config.GetConnectionString("DefaultConnection"); }

        [HttpGet]
        public IActionResult Index()
        {
            var viewModel = new BookingViewModel();
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                // 1. Tải danh sách Đặt bàn
                using (SqlCommand cmd = new SqlCommand("SELECT d.*, b.TenBan, b.KhuVuc FROM DatBan d LEFT JOIN Ban b ON d.MaBan = b.MaBan ORDER BY NgayDat DESC, GioDat DESC", conn))
                {
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            viewModel.Bookings.Add(new DatBanModel
                            {
                                MaDatBan = Convert.ToInt32(r["MaDatBan"]),
                                TenKhachHang = r["TenKhachHang"].ToString(),
                                SoDienThoai = r["SoDienThoai"].ToString(),
                                NgayDat = Convert.ToDateTime(r["NgayDat"]),
                                GioDat = (TimeSpan)r["GioDat"],
                                SoNguoi = Convert.ToInt32(r["SoNguoi"]),
                                GhiChu = r["GhiChu"].ToString(),
                                TrangThai = r["TrangThai"].ToString(),
                                MaBan = r["MaBan"] != DBNull.Value ? Convert.ToInt32(r["MaBan"]) : null,
                                TenBan = r["TenBan"].ToString(),
                                KhuVuc = r["KhuVuc"].ToString()
                            });
                        }
                    }
                }
                // 2. Tải danh sách Bàn trống để xếp bàn
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM Ban WHERE TrangThai = N'Trong'", conn))
                {
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            viewModel.Tables.Add(new BanModel
                            {
                                MaBan = Convert.ToInt32(r["MaBan"]),
                                TenBan = r["TenBan"].ToString(),
                                KhuVuc = r["KhuVuc"].ToString(),
                                SucChua = r["SucChua"] != DBNull.Value ? Convert.ToInt32(r["SucChua"]) : 4
                            });
                        }
                    }
                }
            }
            return View(viewModel); // Trả cục dữ liệu này ra giao diện
        }

        [HttpPost]
        public IActionResult UpdateStatus([FromBody] UpdateStatusRequest req)
        {
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                // Lấy bàn hiện tại đang giữ
                int? currentMaBan = null;
                using (SqlCommand cmd = new SqlCommand("SELECT MaBan FROM DatBan WHERE MaDatBan = @ID", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", req.Id);
                    var result = cmd.ExecuteScalar();
                    if (result != DBNull.Value) currentMaBan = Convert.ToInt32(result);
                }

                if (req.Status == "Đã xác nhận")
                {
                    new SqlCommand($"UPDATE DatBan SET TrangThai = N'Đã xác nhận', MaBan = {req.MaBan} WHERE MaDatBan = {req.Id}", conn).ExecuteNonQuery();
                    new SqlCommand($"UPDATE Ban SET TrangThai = N'Đã đặt' WHERE MaBan = {req.MaBan}", conn).ExecuteNonQuery();
                }
                else if (req.Status == "Đã hủy" || req.Status == "Đã đến")
                {
                    new SqlCommand($"UPDATE DatBan SET TrangThai = N'{req.Status}' WHERE MaDatBan = {req.Id}", conn).ExecuteNonQuery();
                    if (currentMaBan != null)
                    {
                        string ttBan = req.Status == "Đã hủy" ? "Trong" : "Đang phục vụ";
                        new SqlCommand($"UPDATE Ban SET TrangThai = N'{ttBan}' WHERE MaBan = {currentMaBan}", conn).ExecuteNonQuery();
                    }
                }
            }
            return Json(new { success = true, message = "Cập nhật thành công!" });
        }
    }
}