using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace KoiCafe.Controllers
{
    public class TablesController : Controller
    {
        private readonly string _connectionString;

        public TablesController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Tương đương: app.get('/tables')
        [HttpGet]
        public IActionResult Index()
        {
            var tables = new List<Dictionary<string, object>>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM Ban ORDER BY MaBan ASC", conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.GetValue(i);
                            }
                            tables.Add(row);
                        }
                    }
                }
            }

            // Trả về file giao diện Views/Tables/Index.cshtml
            return View(tables);
        }

        // Tương đương: app.post('/tables/add')
        [HttpPost]
        public IActionResult Add(string tenBan, string khuVuc, int sucChua)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "INSERT INTO Ban (TenBan, TrangThai, KhuVuc, SucChua) VALUES (@TenBan, N'Trong', @KhuVuc, @SucChua)";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TenBan", tenBan);
                    cmd.Parameters.AddWithValue("@KhuVuc", string.IsNullOrEmpty(khuVuc) ? (object)DBNull.Value : khuVuc);
                    cmd.Parameters.AddWithValue("@SucChua", sucChua == 0 ? 4 : sucChua);
                    
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }
    }
}