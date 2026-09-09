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

        [HttpPost]
        public IActionResult Create(CreateProductRequest req)
        {
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                // Lưu dữ liệu vào cột MaDM
                string sql = "INSERT INTO SanPham (TenSP, MoTa, DonGia, MaDM, HinhAnh, TrangThai) VALUES (@Ten, @MoTa, @Gia, @MaDM, @Anh, 1)";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Ten", req.TenSP);
                    cmd.Parameters.AddWithValue("@MoTa", req.MoTa ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Gia", req.DonGia);
                    cmd.Parameters.AddWithValue("@MaDM", req.MaDM);
                    cmd.Parameters.AddWithValue("@Anh", req.HinhAnh ?? (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            TempData["Success"] = "✅ Thêm món mới thành công!";
            return RedirectToAction("Index");
        }
    }
}