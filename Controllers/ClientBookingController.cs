using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;

namespace KoiCafe.Controllers
{
    // Controller này độc lập, dành riêng cho phía Khách hàng
    public class ClientBookingController : Controller
    {
        private readonly string _conn;
        public ClientBookingController(IConfiguration config) { _conn = config.GetConnectionString("DefaultConnection"); }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // API nhận dữ liệu từ form khách hàng gửi lên
        [HttpPost]
        public IActionResult SubmitBooking(string tenKhach, string soDienThoai, int soNguoi, DateTime thoiGianDat)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    // Lưu thẳng vào database với trạng thái mặc định là "Chờ xác nhận"
                    string sql = @"INSERT INTO DatBan (TenKhach, SoDienThoai, SoNguoi, ThoiGianDat, TrangThai) 
                                   VALUES (@Ten, @SDT, @SoNguoi, @ThoiGian, N'Chờ xác nhận')";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Ten", tenKhach);
                        cmd.Parameters.AddWithValue("@SDT", soDienThoai);
                        cmd.Parameters.AddWithValue("@SoNguoi", soNguoi);
                        cmd.Parameters.AddWithValue("@ThoiGian", thoiGianDat);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { success = true, message = "🎉 Gửi yêu cầu đặt bàn thành công! Quản lý sẽ sớm xác nhận cho bạn." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}