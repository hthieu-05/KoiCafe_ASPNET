using KoiCafe.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KoiCafe.Controllers
{
    [AdminAuth] 
    public class DashboardController : Controller
    {
        private readonly string _conn;
        public DashboardController(IConfiguration config) { _conn = config.GetConnectionString("DefaultConnection"); }

        public IActionResult Index()
        {
            ViewBag.TenNV = HttpContext.Session.GetString("TenNV");

            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                
                // 1. Tính tổng Doanh thu trong ngày hôm nay
                using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(TongTien), 0) FROM HoaDon WHERE TrangThai = N'Đã thanh toán' AND CAST(NgayLap AS DATE) = CAST(GETDATE() AS DATE)", conn))
                {
                    ViewBag.DoanhThu = (decimal)cmd.ExecuteScalar();
                }

                // 2. Đếm số đơn hàng thành công hôm nay
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM HoaDon WHERE TrangThai = N'Đã thanh toán' AND CAST(NgayLap AS DATE) = CAST(GETDATE() AS DATE)", conn))
                {
                    ViewBag.TongDon = (int)cmd.ExecuteScalar();
                }

                // 3. Đếm số bàn đang bận (đang phục vụ)
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Ban WHERE TrangThai = N'Đang phục vụ'", conn))
                {
                    ViewBag.BanBan = (int)cmd.ExecuteScalar();
                }
            }

            return View();
        }
    }
}