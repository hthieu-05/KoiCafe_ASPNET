using KoiCafe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Http; // Bắt buộc thêm thư viện này để dùng Session

namespace KoiCafe.Controllers
{
    // ĐÃ XÓA BỎ THẺ [AdminAuth] Ở ĐÂY
    public class MenuController : Controller
    {
        private readonly string _conn;
        public MenuController(IConfiguration config) { _conn = config.GetConnectionString("DefaultConnection"); }

        [HttpGet]
        public IActionResult Index()
        {
            // 1. KIỂM TRA PHÂN QUYỀN BẰNG SESSION
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            
            // Nếu chưa đăng nhập -> Đẩy ra trang Login
            if (string.IsNullOrEmpty(vaiTro)) 
            {
                return RedirectToAction("Index", "Login");
            }
            
            // Nếu là Nhân viên -> Không cho vào, tự động đẩy về trang Máy Bán Hàng (POS)
            if (vaiTro == "Nhân viên") 
            {
                return RedirectToAction("Index", "Pos");
            }

            // 2. NẾU LÀ ADMIN HOẶC QUẢN LÝ -> CHO PHÉP ĐI TIẾP VÀ TẢI DỮ LIỆU
            var products = new List<Dictionary<string, object>>();
            var categories = new List<Dictionary<string, object>>();

            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                // 1. Kéo Sản phẩm kèm theo Tên Danh Mục (TenDM)
                string sqlSp = "SELECT sp.*, dm.TenDM FROM SanPham sp LEFT JOIN DanhMuc dm ON sp.MaDM = dm.MaDM ORDER BY dm.TenDM, sp.TenSP";
                using (SqlCommand cmd = new SqlCommand(sqlSp, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++) row[reader.GetName(i)] = reader.GetValue(i);
                        products.Add(row);
                    }
                }

                // 2. Kéo danh sách Danh Mục truyền ra Thẻ Select trong Form Thêm Món
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM DanhMuc", conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(new Dictionary<string, object> {
                            { "MaDM", reader["MaDM"] },
                            { "TenDM", reader["TenDM"] }
                        });
                    }
                }
            }
            
            ViewBag.Categories = categories; 
            return View(products);
        }

        
        // [GET] API: Kéo dữ liệu 
        [HttpGet]
        public IActionResult GetProduct(int id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    string sql = "SELECT * FROM SanPham WHERE MaSP = @id";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                return Json(new { 
                                    success = true, 
                                    data = new {
                                        maSP = r["MaSP"], 
                                        tenSP = r["TenSP"].ToString(), 
                                        maDM = r["MaDM"] != DBNull.Value ? r["MaDM"] : "", 
                                        donGia = r["DonGia"], 
                                        hinhAnh = r["HinhAnh"] != DBNull.Value ? r["HinhAnh"].ToString() : "", 
                                        trangThai = r["TrangThai"] != DBNull.Value ? Convert.ToBoolean(r["TrangThai"]) : true
                                    } 
                                });
                            }
                        }
                    }
                }
                return Json(new { success = false, message = "Không tìm thấy món ăn!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // [POST] Sửa món ăn 
        [HttpPost]
        public IActionResult Edit(UpdateProductRequest req)
        {
            // Bảo mật thêm ở vòng lưu dữ liệu: Chặn Nhân viên lén gửi request sửa món
            if (HttpContext.Session.GetString("VaiTro") == "Nhân viên") return RedirectToAction("Index", "Pos");

            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                string sql = "UPDATE SanPham SET TenSP=@Ten, DonGia=@Gia, MaDM=@MaDM, HinhAnh=@Anh, TrangThai=@TrangThai WHERE MaSP=@MaSP";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Ten", req.TenSP);
                    cmd.Parameters.AddWithValue("@Gia", req.DonGia);
                    cmd.Parameters.AddWithValue("@MaDM", req.MaDM);
                    cmd.Parameters.AddWithValue("@Anh", req.HinhAnh ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@TrangThai", req.TrangThai ? 1 : 0);
                    cmd.Parameters.AddWithValue("@MaSP", req.MaSP);
                    cmd.ExecuteNonQuery();
                }
            }
            TempData["Success"] = "✅ Cập nhật món ăn thành công!";
            return RedirectToAction("Index");
        }

        // [POST] Thêm món ăn
        [HttpPost]
        public IActionResult Create(CreateProductRequest req)
        {
            if (HttpContext.Session.GetString("VaiTro") == "Nhân viên") return RedirectToAction("Index", "Pos");

            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                string sql = "INSERT INTO SanPham (TenSP, DonGia, MaDM, HinhAnh, TrangThai) VALUES (@Ten, @Gia, @MaDM, @Anh, 1)";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Ten", req.TenSP);
                    cmd.Parameters.AddWithValue("@Gia", req.DonGia);
                    cmd.Parameters.AddWithValue("@MaDM", req.MaDM);
                    cmd.Parameters.AddWithValue("@Anh", req.HinhAnh ?? (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            TempData["Success"] = "✅ Thêm món mới thành công!";
            return RedirectToAction("Index");
        }

        // [POST] Xóa món ăn
        [HttpPost]
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetString("VaiTro") == "Nhân viên") return RedirectToAction("Index", "Pos");

            try
            {
                using (SqlConnection conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM SanPham WHERE MaSP = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["Success"] = "✅ Đã xóa món ăn!";
            }
            catch (Exception)
            {
                TempData["Error"] = "❌ Không thể xóa! Món ăn này đã tồn tại trong lịch sử Hóa đơn. Hãy chọn Cập nhật -> Ngừng bán.";
            }
            return RedirectToAction("Index");
        }
    }
}