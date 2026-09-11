using KoiCafe.Filters;
using KoiCafe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KoiCafe.Controllers
{
    [AdminAuth]
    public class MenuController : Controller
    {
        private readonly string _conn;
        public MenuController(IConfiguration config) { _conn = config.GetConnectionString("DefaultConnection"); }

        [HttpGet]
        public IActionResult Index()
        {
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
            
            // Gửi danh mục ra View thông qua ViewBag
            ViewBag.Categories = categories; 
            return View(products);
        }

        
        // [GET] API: Kéo dữ liệu (Đã xóa cột MoTa)
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

        // [POST] Sửa món ăn (Đã xóa cột MoTa)
        [HttpPost]
        public IActionResult Edit(UpdateProductRequest req)
        {
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

        // [POST] Thêm món ăn (Đã xóa cột MoTa để tránh lỗi khi thêm mới)
        [HttpPost]
        public IActionResult Create(CreateProductRequest req)
        {
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
            try
            {
                using (SqlConnection conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    // Lưu ý: Nếu món này đã có trong Hóa đơn thì hệ thống SQL sẽ chặn không cho xóa để bảo toàn dữ liệu. 
                    // Lúc đó bạn chỉ nên Sửa trạng thái thành "Ngừng bán".
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