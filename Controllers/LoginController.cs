using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KoiCafe.Controllers
{
    public class LoginController : Controller
    {
        private readonly string _connString;
        public LoginController(IConfiguration config) { _connString = config.GetConnectionString("DefaultConnection"); }

        [HttpGet]
        public IActionResult Index() { return View(); }

        [HttpPost]
        public IActionResult Index(string username, string password)
        {
            using (SqlConnection conn = new SqlConnection(_connString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT tk.*, nv.TenNV FROM TaiKhoan tk JOIN NhanVien nv ON tk.MaNV = nv.MaNV WHERE tk.TenDangNhap = @User AND tk.MatKhau = @Pass", conn))
                {
                    cmd.Parameters.AddWithValue("@User", username);
                    cmd.Parameters.AddWithValue("@Pass", password);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            HttpContext.Session.SetString("TenNV", reader["TenNV"].ToString());
                            HttpContext.Session.SetString("VaiTro", reader["VaiTro"].ToString());
                            
                            if (reader["VaiTro"].ToString() == "Admin") return RedirectToAction("Index", "Dashboard");
                            else return RedirectToAction("Index", "Pos");
                        }
                    }
                }
            }
            ViewBag.Error = "Sai tài khoản hoặc mật khẩu!";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}