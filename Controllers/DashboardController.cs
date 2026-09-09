using KoiCafe.Filters;
using Microsoft.AspNetCore.Mvc;

namespace KoiCafe.Controllers
{
    // Bắt buộc đăng nhập với quyền Admin mới được vào trang này
    [AdminAuth] 
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            // Lấy tên nhân viên từ Session để hiển thị ra màn hình
            ViewBag.TenNV = HttpContext.Session.GetString("TenNV");
            return View();
        }
    }
}