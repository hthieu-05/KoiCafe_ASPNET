using KoiCafe.Filters;
using KoiCafe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KoiCafe.Controllers
{
    [AdminAuth]
    public class StaffController : Controller
    {
        private readonly string _conn;
        public StaffController(IConfiguration config) { _conn = config.GetConnectionString("DefaultConnection"); }

        [HttpGet]
        public IActionResult Index()
        {
            var staffs = new List<NhanVienModel>();

            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                // ĐÃ SỬA: Lấy tk.VaiTro từ bảng TaiKhoan thay vì nv.VaiTro
                string sql = "SELECT nv.MaNV, nv.TenNV, nv.SoDienThoai, tk.VaiTro, tk.TenDangNhap FROM NhanVien nv LEFT JOIN TaiKhoan tk ON nv.MaNV = tk.MaNV";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            staffs.Add(new NhanVienModel
                            {
                                MaNV = Convert.ToInt32(reader["MaNV"]),
                                TenNV = reader["TenNV"].ToString(),
                                SoDienThoai = reader["SoDienThoai"].ToString(),
                                VaiTro = reader["VaiTro"] != DBNull.Value ? reader["VaiTro"].ToString() : "Chưa phân quyền",
                                TenDangNhap = reader["TenDangNhap"] != DBNull.Value ? reader["TenDangNhap"].ToString() : "Chưa cấp"
                            });
                        }
                    }
                }
            }
            return View(staffs);
        }

        [HttpPost]
        public IActionResult Create(CreateStaffRequest req)
        {
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                
                // ĐÃ SỬA: Bỏ cột VaiTro khỏi bảng NhanVien
                string insertNvSql = "INSERT INTO NhanVien (TenNV, SoDienThoai) OUTPUT INSERTED.MaNV VALUES (@Ten, @Sdt)";
                int newMaNV;
                using (SqlCommand cmd1 = new SqlCommand(insertNvSql, conn))
                {
                    cmd1.Parameters.AddWithValue("@Ten", req.TenNV);
                    cmd1.Parameters.AddWithValue("@Sdt", req.SoDienThoai ?? (object)DBNull.Value);
                    newMaNV = (int)cmd1.ExecuteScalar(); 
                }

                if (!string.IsNullOrEmpty(req.TenDangNhap) && !string.IsNullOrEmpty(req.MatKhau))
                {
                    // ĐÃ SỬA: Thêm cột VaiTro vào bảng TaiKhoan
                    string insertTkSql = "INSERT INTO TaiKhoan (TenDangNhap, MatKhau, VaiTro, MaNV) VALUES (@User, @Pass, @VaiTro, @MaNV)";
                    using (SqlCommand cmd2 = new SqlCommand(insertTkSql, conn))
                    {
                        cmd2.Parameters.AddWithValue("@User", req.TenDangNhap);
                        cmd2.Parameters.AddWithValue("@Pass", req.MatKhau); 
                        cmd2.Parameters.AddWithValue("@VaiTro", req.VaiTro); // Lưu vai trò vào Tài khoản
                        cmd2.Parameters.AddWithValue("@MaNV", newMaNV);
                        cmd2.ExecuteNonQuery();
                    }
                }
            }
            TempData["Success"] = "✅ Thêm nhân sự và cấp tài khoản thành công!";
            return RedirectToAction("Index");
        }
    }
}