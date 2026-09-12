using KoiCafe.Filters;
using KoiCafe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KoiCafe.Controllers
{
    [AdminAuth]
    public class InvoiceController : Controller
    {
        private readonly string _conn;
        public InvoiceController(IConfiguration config) { _conn = config.GetConnectionString("DefaultConnection"); }

        // [GET] Lấy danh sách Hóa đơn CHƯA THANH TOÁN
        [HttpGet]
        public IActionResult Index()
        {
            var activeInvoices = new List<HoaDonModel>();
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                string sql = @"SELECT hd.MaHD, b.TenBan, b.MaBan, nv.TenNV, hd.NgayLap, hd.TongTien 
                               FROM HoaDon hd 
                               LEFT JOIN Ban b ON hd.MaBan = b.MaBan 
                               LEFT JOIN NhanVien nv ON hd.MaNV = nv.MaNV 
                               WHERE hd.TrangThai = N'Chưa thanh toán'
                               ORDER BY hd.NgayLap ASC";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        activeInvoices.Add(new HoaDonModel
                        {
                            MaHD = Convert.ToInt32(reader["MaHD"]),
                            MaBan = reader["MaBan"] != DBNull.Value ? Convert.ToInt32(reader["MaBan"]) : 0,
                            TenBan = reader["TenBan"] != DBNull.Value ? reader["TenBan"].ToString() : "Mang đi",
                            TenNV = reader["TenNV"] != DBNull.Value ? reader["TenNV"].ToString() : "Hệ thống",
                            NgayLap = Convert.ToDateTime(reader["NgayLap"]),
                            TongTien = Convert.ToDecimal(reader["TongTien"])
                        });
                    }
                }
            }
            return View(activeInvoices);
        }

        // [POST] Xử lý thanh toán và lưu vết
        [HttpPost]
        public IActionResult ProcessInvoice([FromBody] InvoiceProcessRequest req)
        {
            string tenNV = HttpContext.Session.GetString("TenNV") ?? "Hệ thống";

            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                
                // 1. Kiểm tra trạng thái (Khóa hóa đơn)
                using (SqlCommand checkCmd = new SqlCommand("SELECT TrangThai FROM HoaDon WHERE MaHD = @MaHD", conn))
                {
                    checkCmd.Parameters.AddWithValue("@MaHD", req.MaHD);
                    var status = checkCmd.ExecuteScalar()?.ToString();
                    if (status == "Đã thanh toán" || status == "Đã hủy") 
                        return Json(new { success = false, message = "Hóa đơn đã khóa, không thể thao tác!" });
                }

                // Bắt đầu Transaction để bảo toàn dữ liệu tài chính
                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    if (req.HanhDong == "ThanhToan")
                    {
                        // 2. Chốt tiền & Cập nhật người sửa
                        string sqlHD = "UPDATE HoaDon SET TrangThai = N'Đã thanh toán', TongTien = (TongTien - @KM), NguoiSuaCuoi = @NV WHERE MaHD = @MaHD";
                        using (SqlCommand cmd = new SqlCommand(sqlHD, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@KM", req.SoTienKhuyenMai);
                            cmd.Parameters.AddWithValue("@NV", tenNV);
                            cmd.Parameters.AddWithValue("@MaHD", req.MaHD);
                            cmd.ExecuteNonQuery();
                        }

                        // 3. Tự trả bàn về Trống
                        string sqlBan = "UPDATE Ban SET TrangThai = N'Trong' WHERE MaBan = @MaBan";
                        using (SqlCommand cmd = new SqlCommand(sqlBan, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@MaBan", req.MaBan);
                            cmd.ExecuteNonQuery();
                        }

                        // 4. Lưu lịch sử thao tác
                        string sqlLog = "INSERT INTO LichSuThaoTac (MaHD, TenNhanVien, ThoiGian, HanhDong) VALUES (@MaHD, @NV, GETDATE(), N'Thanh toán hóa đơn')";
                        using (SqlCommand cmd = new SqlCommand(sqlLog, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@MaHD", req.MaHD);
                            cmd.Parameters.AddWithValue("@NV", tenNV);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    
                    transaction.Commit();
                    return Json(new { success = true, message = "Xử lý hóa đơn thành công!" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); // Hoàn tác nếu có bất kỳ lỗi gì
                    return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
                }
            }
        }
    }
}