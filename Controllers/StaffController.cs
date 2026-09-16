using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Http;

namespace KoiCafe.Controllers
{
    // 1. LÁ CHẮN BẢO MẬT (Chỉ Admin mới được vào trang này)
    public class AdminOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var vaiTro = context.HttpContext.Session.GetString("VaiTro");
            if (string.IsNullOrEmpty(vaiTro) || vaiTro != "Admin") // Đã bỏ "Quản lý"
            {
                context.Result = new RedirectResult("/Pos"); 
            }
            base.OnActionExecuting(context);
        }
    }

    public class NhanVienModel
    {
        public int MaNV { get; set; }
        public string TenNV { get; set; }
        public string SoDienThoai { get; set; }
        public string VaiTro { get; set; }
        public string TenDangNhap { get; set; }
    }

    [AdminOnly] 
    public class StaffController : Controller
    {
        private readonly string _conn;
        public StaffController(IConfiguration config) { _conn = config.GetConnectionString("DefaultConnection"); }

        [HttpGet]
        public IActionResult Index()
        {
            var dsNV = new List<NhanVienModel>();
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM NhanVien ORDER BY VaiTro, TenNV", conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while(r.Read())
                    {
                        dsNV.Add(new NhanVienModel {
                            MaNV = Convert.ToInt32(r["MaNV"]),
                            TenNV = r["TenNV"].ToString(),
                            SoDienThoai = r["SoDienThoai"] != DBNull.Value ? r["SoDienThoai"].ToString() : "",
                            VaiTro = r["VaiTro"].ToString(),
                            TenDangNhap = r["TenDangNhap"] != DBNull.Value ? r["TenDangNhap"].ToString() : ""
                        });
                    }
                }
            }
            return View(dsNV);
        }

        [HttpPost]
        public IActionResult AddStaff(string tenNV, string sdt, string vaiTro, string username, string password)
        {
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                var check = new SqlCommand("SELECT COUNT(1) FROM NhanVien WHERE TenDangNhap = @User", conn);
                check.Parameters.AddWithValue("@User", username);
                if ((int)check.ExecuteScalar() > 0)
                {
                    TempData["Error"] = "Tên đăng nhập này đã có người sử dụng!";
                    return RedirectToAction("Index");
                }

                string sql = "INSERT INTO NhanVien (TenNV, SoDienThoai, VaiTro, TenDangNhap, MatKhau) VALUES (@Ten, @SDT, @VaiTro, @User, @Pass)";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Ten", tenNV);
                    cmd.Parameters.AddWithValue("@SDT", sdt ?? "");
                    cmd.Parameters.AddWithValue("@VaiTro", vaiTro);
                    cmd.Parameters.AddWithValue("@User", username);
                    cmd.Parameters.AddWithValue("@Pass", password);
                    cmd.ExecuteNonQuery();
                }
            }
            TempData["Success"] = "Đã tạo tài khoản nhân viên thành công!";
            return RedirectToAction("Index");
        }

        // [MỚI] CHỨC NĂNG SỬA THÔNG TIN & ĐỔI MẬT KHẨU
        [HttpPost]
        public IActionResult EditStaff(int maNV, string tenNV, string sdt, string vaiTro, string password)
        {
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                // Nếu có nhập mật khẩu mới thì cập nhật cả mật khẩu, nếu để trống thì giữ nguyên mật khẩu cũ
                string sql = string.IsNullOrEmpty(password) 
                    ? "UPDATE NhanVien SET TenNV=@Ten, SoDienThoai=@SDT, VaiTro=@VaiTro WHERE MaNV=@MaNV"
                    : "UPDATE NhanVien SET TenNV=@Ten, SoDienThoai=@SDT, VaiTro=@VaiTro, MatKhau=@Pass WHERE MaNV=@MaNV";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Ten", tenNV);
                    cmd.Parameters.AddWithValue("@SDT", sdt ?? "");
                    cmd.Parameters.AddWithValue("@VaiTro", vaiTro);
                    if (!string.IsNullOrEmpty(password)) cmd.Parameters.AddWithValue("@Pass", password);
                    cmd.Parameters.AddWithValue("@MaNV", maNV);
                    cmd.ExecuteNonQuery();
                }
            }
            TempData["Success"] = "Đã cập nhật thông tin tài khoản thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteStaff(int id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    
                    // 1. Gỡ mã nhân viên khỏi các Hóa Đơn cũ (Phòng hờ lỗi ràng buộc Hóa đơn)
                    try { 
                        new SqlCommand($"UPDATE HoaDon SET MaNV = NULL WHERE MaNV = {id}", conn).ExecuteNonQuery(); 
                    } catch { }

                    // 2. DIỆT THỦ PHẠM GÂY LỖI: Xóa dữ liệu liên quan trong bảng TaiKhoan trước
                    try { 
                        new SqlCommand($"DELETE FROM TaiKhoan WHERE MaNV = {id}", conn).ExecuteNonQuery(); 
                    } catch { }

                    // 3. Tiến hành trảm tận gốc tài khoản trong bảng NhanVien
                    new SqlCommand($"DELETE FROM NhanVien WHERE MaNV = {id}", conn).ExecuteNonQuery();
                }
                return Json(new { success = true, message = "✅ Đã xóa tận gốc tài khoản và các dữ liệu liên quan!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}