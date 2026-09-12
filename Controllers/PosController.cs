using KoiCafe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KoiCafe.Controllers
{
    public class PosController : Controller
    {
        private readonly string _conn;
        public PosController(IConfiguration config) { _conn = config.GetConnectionString("DefaultConnection"); }

        // [GET] Tải giao diện POS
        [HttpGet]
        public IActionResult Index()
        {
            // Kiểm tra xem đã đăng nhập chưa (Thu ngân hoặc Admin đều được vào)
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("VaiTro"))) 
                return RedirectToAction("Index", "Login");

            var vm = new PosViewModel();
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                // 1. Tải danh sách Bàn
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM Ban", conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read()) vm.Tables.Add(new BanModel { 
                        MaBan = Convert.ToInt32(r["MaBan"]), TenBan = r["TenBan"].ToString(), 
                        KhuVuc = r["KhuVuc"].ToString(), TrangThai = r["TrangThai"].ToString() 
                    });
                }
                // 2. Tải danh sách Sản phẩm đang bán
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM SanPham WHERE TrangThai = 1", conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read()) vm.Products.Add(new SanPhamModel { 
                        MaSP = Convert.ToInt32(r["MaSP"]), TenSP = r["TenSP"].ToString(), 
                        DonGia = Convert.ToDecimal(r["DonGia"]), HinhAnh = r["HinhAnh"].ToString() 
                    });
                }
            }
            return View(vm);
        }

        // [GET] API Lấy món ăn của bàn đang phục vụ
        [HttpGet]
        public IActionResult GetTableOrder(int id)
        {
            var cart = new List<object>();
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                string sql = "SELECT ct.MaSP, sp.TenSP, ct.DonGia, ct.SoLuong FROM ChiTietHoaDon ct JOIN HoaDon hd ON ct.MaHD = hd.MaHD JOIN SanPham sp ON ct.MaSP = sp.MaSP WHERE hd.MaBan = @MaBan AND hd.TrangThai = N'Chưa thanh toán'";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MaBan", id);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read()) cart.Add(new { 
                            maSP = r["MaSP"], tenSP = r["TenSP"], donGia = r["DonGia"], soLuong = r["SoLuong"] 
                        });
                    }
                }
            }
            return Json(new { success = true, cart });
        }

        // [POST] Xử lý Đặt món / Thanh toán / Hủy
        [HttpPost]
        public IActionResult ProcessOrder([FromBody] PosRequest req)
        {
            string tenThuNgan = HttpContext.Session.GetString("TenNV") ?? "Hệ thống";

            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                
                // 1. Kiểm tra xem bàn có hóa đơn chưa thanh toán nào không
                int maHD = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT MaHD FROM HoaDon WHERE MaBan = @MaBan AND TrangThai = N'Chưa thanh toán'", conn))
                {
                    cmd.Parameters.AddWithValue("@MaBan", req.MaBan);
                    var result = cmd.ExecuteScalar();
                    if (result != null) maHD = Convert.ToInt32(result);
                }

                // HỦY HÓA ĐƠN
                if (req.Action == "cancel")
                {
                    if (maHD == 0) return Json(new { success = false, message = "Bàn này không có Hóa đơn để hủy!" });
                    new SqlCommand($"UPDATE HoaDon SET TrangThai = N'Đã hủy', LyDoHuy = N'{req.LyDoHuy}' WHERE MaHD = {maHD}", conn).ExecuteNonQuery();
                    new SqlCommand($"UPDATE Ban SET TrangThai = N'Trong' WHERE MaBan = {req.MaBan}", conn).ExecuteNonQuery();
                    // Lưu Log
                    new SqlCommand($"INSERT INTO LichSuThaoTac (MaHD, TenNhanVien, HanhDong) VALUES ({maHD}, N'{tenThuNgan}', N'Hủy hóa đơn. Lý do: {req.LyDoHuy}')", conn).ExecuteNonQuery();
                    return Json(new { success = true, message = "Đã hủy hóa đơn!" });
                }

                // LƯU HOẶC THANH TOÁN
                decimal tongTien = req.Cart.Sum(i => i.DonGia * i.SoLuong);
                string trangThaiHD = req.Action == "pay" ? "Đã thanh toán" : "Chưa thanh toán";
                string trangThaiBan = req.Action == "pay" ? "Trong" : "Đang phục vụ";

                // Xử lý Hóa Đơn (Tạo mới hoặc Cập nhật)
                if (maHD == 0)
                {
                    using (SqlCommand cmd = new SqlCommand("INSERT INTO HoaDon (MaBan, NgayLap, TongTien, TrangThai) OUTPUT INSERTED.MaHD VALUES (@MaBan, GETDATE(), @TongTien, @TrangThai)", conn))
                    {
                        cmd.Parameters.AddWithValue("@MaBan", req.MaBan);
                        cmd.Parameters.AddWithValue("@TongTien", tongTien);
                        cmd.Parameters.AddWithValue("@TrangThai", trangThaiHD);
                        maHD = (int)cmd.ExecuteScalar();
                    }
                }
                else
                {
                    new SqlCommand($"UPDATE HoaDon SET TongTien = {tongTien}, TrangThai = N'{trangThaiHD}' WHERE MaHD = {maHD}", conn).ExecuteNonQuery();
                    new SqlCommand($"DELETE FROM ChiTietHoaDon WHERE MaHD = {maHD}", conn).ExecuteNonQuery(); // Xóa chi tiết cũ để nạp lại
                }

                // Nạp chi tiết món ăn
                foreach (var item in req.Cart)
                {
                    using (SqlCommand cmd = new SqlCommand("INSERT INTO ChiTietHoaDon (MaHD, MaSP, SoLuong, DonGia) VALUES (@MaHD, @MaSP, @SL, @Gia)", conn))
                    {
                        cmd.Parameters.AddWithValue("@MaHD", maHD);
                        cmd.Parameters.AddWithValue("@MaSP", item.MaSP);
                        cmd.Parameters.AddWithValue("@SL", item.SoLuong);
                        cmd.Parameters.AddWithValue("@Gia", item.DonGia);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Cập nhật Bàn và Lưu Log
                new SqlCommand($"UPDATE Ban SET TrangThai = N'{trangThaiBan}' WHERE MaBan = {req.MaBan}", conn).ExecuteNonQuery();
                string logAction = req.Action == "pay" ? "Thanh toán hóa đơn" : "Lưu món ăn";
                new SqlCommand($"INSERT INTO LichSuThaoTac (MaHD, TenNhanVien, HanhDong) VALUES ({maHD}, N'{tenThuNgan}', N'{logAction}')", conn).ExecuteNonQuery();

                return Json(new { success = true, message = req.Action == "pay" ? "✅ Thu tiền thành công!" : "✅ Đã lưu món!" });
            }
        }
    }
}