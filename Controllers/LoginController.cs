using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

namespace KoiCafe.Controllers
{
    public class LoginController : Controller
    {
        private readonly string _conn;
        
        public LoginController(IConfiguration config) 
        { 
            _conn = config.GetConnectionString("DefaultConnection"); 
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Nếu đã đăng nhập rồi thì tự động chuyển vào trong
            var vaiTroHienTai = HttpContext.Session.GetString("VaiTro");
            if (!string.IsNullOrEmpty(vaiTroHienTai))
            {
                if (vaiTroHienTai == "Nhân viên") return RedirectToAction("Index", "Pos");
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Index(string username, string password) 
        {
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                string sql = "SELECT TenNV, VaiTro FROM NhanVien WHERE TenDangNhap = @user AND MatKhau = @pass";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    // Truyền tham số để đối chiếu với Database
                    cmd.Parameters.AddWithValue("@user", username);
                    cmd.Parameters.AddWithValue("@pass", password);
                    
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read()) // Nếu khớp tài khoản và mật khẩu
                        {
                            string chucVu = r["VaiTro"].ToString();
                            
                            // LƯU SESSION ĐỂ HIỂN THỊ MENU PHÂN QUYỀN
                            HttpContext.Session.SetString("TenNV", r["TenNV"].ToString());
                            HttpContext.Session.SetString("VaiTro", chucVu);
                            
                            // ĐIỀU HƯỚNG THÔNG MINH
                            if (chucVu == "Nhân viên")
                            {
                                return RedirectToAction("Index", "Pos"); // Nhân viên vào máy POS
                            }
                            else
                            {
                                return RedirectToAction("Index", "Dashboard"); // Quản lý/Admin vào Báo cáo
                            }
                        }
                    }
                }
            }
            
            // Nếu không khớp (sai acc/pass)
            ViewBag.Error = "Sai tài khoản hoặc mật khẩu!";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }
    }
}