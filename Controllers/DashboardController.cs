using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http; // Thêm thư viện này để đọc Session

namespace KoiCafe.Controllers
{
    // Đã gỡ bỏ [AdminAuth] gây lỗi ở đây
    public class DashboardController : Controller
    {
        private readonly string _conn;
        public DashboardController(IConfiguration config) { _conn = config.GetConnectionString("DefaultConnection"); }

        public IActionResult Index()
        {
            // 1. KIỂM TRA QUYỀN BẰNG SESSION
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            
            // Nếu chưa đăng nhập -> Về trang Login
            if (string.IsNullOrEmpty(vaiTro))
            {
                return RedirectToAction("Index", "Login");
            }

            // Nếu là Nhân viên -> Đẩy về trang Máy bán hàng (Không cho xem báo cáo)
            if (vaiTro == "Nhân viên")
            {
                return RedirectToAction("Index", "Pos"); 
            }

            // 2. ADMIN VÀ QUẢN LÝ ĐƯỢC ĐI TIẾP ĐỂ LẤY DỮ LIỆU BÁO CÁO
            ViewBag.TenNV = HttpContext.Session.GetString("TenNV");

            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                
                // Tính tổng Doanh thu trong ngày hôm nay
                using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(TongTien), 0) FROM HoaDon WHERE TrangThai = N'Đã thanh toán' AND CAST(NgayLap AS DATE) = CAST(GETDATE() AS DATE)", conn))
                {
                    ViewBag.DoanhThu = (decimal)cmd.ExecuteScalar();
                }

                // Đếm số đơn hàng thành công hôm nay
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM HoaDon WHERE TrangThai = N'Đã thanh toán' AND CAST(NgayLap AS DATE) = CAST(GETDATE() AS DATE)", conn))
                {
                    ViewBag.TongDon = (int)cmd.ExecuteScalar();
                }

                // Đếm số bàn đang bận (đang phục vụ)
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Ban WHERE TrangThai = N'Đang phục vụ'", conn))
                {
                    ViewBag.BanBan = (int)cmd.ExecuteScalar();
                }
            }

            return View();
        }
    }
}