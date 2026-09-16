using KoiCafe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System;
using System.Linq;

namespace KoiCafe.Controllers
{
    // Lớp mô hình nhận dữ liệu Giỏ hàng có thêm thuộc tính GhiChu
    public class PosCartItem
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public string GhiChu { get; set; }
    }

    // Lớp nhận Request thanh toán có thêm thuộc tính Số điện thoại khách
    public class PosActionRequest
    {
        public int MaBan { get; set; }
        public string Action { get; set; }
        public string LyDoHuy { get; set; }
        public string SdtKhach { get; set; }
        public List<PosCartItem> Cart { get; set; } = new List<PosCartItem>();
    }

    public class PosController : Controller
    {
        private readonly string _conn;
        public PosController(IConfiguration config) { _conn = config.GetConnectionString("DefaultConnection"); }

        [HttpGet]
        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("VaiTro"))) 
                return RedirectToAction("Index", "Login");

            var vm = new PosViewModel();
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM Ban", conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read()) vm.Tables.Add(new BanModel { 
                        MaBan = Convert.ToInt32(r["MaBan"]), TenBan = r["TenBan"].ToString(), 
                        KhuVuc = r["KhuVuc"].ToString(), TrangThai = r["TrangThai"].ToString() 
                    });
                }
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

        [HttpGet]
        public IActionResult GetTableOrder(int id)
        {
            var cart = new List<object>();
            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                // Lấy thêm GhiChu từ DB
                string sql = "SELECT ct.MaSP, sp.TenSP, ct.DonGia, ct.SoLuong, ct.GhiChu FROM ChiTietHoaDon ct JOIN HoaDon hd ON ct.MaHD = hd.MaHD JOIN SanPham sp ON ct.MaSP = sp.MaSP WHERE hd.MaBan = @MaBan AND hd.TrangThai = N'Chưa thanh toán'";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MaBan", id);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read()) cart.Add(new { 
                            maSP = r["MaSP"], tenSP = r["TenSP"], donGia = r["DonGia"], 
                            soLuong = r["SoLuong"], ghiChu = r["GhiChu"].ToString() 
                        });
                    }
                }
            }
            return Json(new { success = true, cart });
        }

        [HttpPost]
        public IActionResult ProcessOrder([FromBody] PosActionRequest req)
        {
            string tenThuNgan = HttpContext.Session.GetString("TenNV") ?? "Hệ thống";

            using (SqlConnection conn = new SqlConnection(_conn))
            {
                conn.Open();
                int maHD = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT MaHD FROM HoaDon WHERE MaBan = @MaBan AND TrangThai = N'Chưa thanh toán'", conn))
                {
                    cmd.Parameters.AddWithValue("@MaBan", req.MaBan);
                    var result = cmd.ExecuteScalar();
                    if (result != null) maHD = Convert.ToInt32(result);
                }

                if (req.Action == "cancel")
                {
                    if (maHD == 0) return Json(new { success = false, message = "Bàn này không có Hóa đơn để hủy!" });
                    new SqlCommand($"UPDATE HoaDon SET TrangThai = N'Đã hủy', LyDoHuy = N'{req.LyDoHuy}' WHERE MaHD = {maHD}", conn).ExecuteNonQuery();
                    new SqlCommand($"UPDATE Ban SET TrangThai = N'Trong' WHERE MaBan = {req.MaBan}", conn).ExecuteNonQuery();
                    new SqlCommand($"INSERT INTO LichSuThaoTac (MaHD, TenNhanVien, HanhDong) VALUES ({maHD}, N'{tenThuNgan}', N'Hủy hóa đơn. Lý do: {req.LyDoHuy}')", conn).ExecuteNonQuery();
                    return Json(new { success = true, message = "Đã hủy hóa đơn!" });
                }

                // TÌM MÃ KHÁCH HÀNG (Dựa vào SĐT thu ngân nhập)
                int maKH = 0;
                if (!string.IsNullOrEmpty(req.SdtKhach))
                {
                    var khResult = new SqlCommand($"SELECT MaKH FROM KhachHang WHERE SoDienThoai = '{req.SdtKhach}'", conn).ExecuteScalar();
                    if (khResult != null) maKH = Convert.ToInt32(khResult);
                }

                decimal tongTien = req.Cart.Sum(i => i.DonGia * i.SoLuong);
                string trangThaiHD = req.Action == "pay" ? "Đã thanh toán" : "Chưa thanh toán";
                string trangThaiBan = req.Action == "pay" ? "Trong" : "Đang phục vụ";

                if (maHD == 0)
                {
                    string sqlInsert = "INSERT INTO HoaDon (MaBan, NgayLap, TongTien, TrangThai, MaKH) OUTPUT INSERTED.MaHD VALUES (@MaBan, GETDATE(), @TongTien, @TrangThai, @MaKH)";
                    using (SqlCommand cmd = new SqlCommand(sqlInsert, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaBan", req.MaBan);
                        cmd.Parameters.AddWithValue("@TongTien", tongTien);
                        cmd.Parameters.AddWithValue("@TrangThai", trangThaiHD);
                        cmd.Parameters.AddWithValue("@MaKH", maKH == 0 ? DBNull.Value : maKH);
                        maHD = (int)cmd.ExecuteScalar();
                    }
                }
                else
                {
                    string sqlUpdate = $"UPDATE HoaDon SET TongTien = {tongTien}, TrangThai = N'{trangThaiHD}', MaKH = {(maKH == 0 ? "NULL" : maKH.ToString())} WHERE MaHD = {maHD}";
                    new SqlCommand(sqlUpdate, conn).ExecuteNonQuery();
                    new SqlCommand($"DELETE FROM ChiTietHoaDon WHERE MaHD = {maHD}", conn).ExecuteNonQuery(); 
                }

                foreach (var item in req.Cart)
                {
                    // Lưu thêm Ghi chú món ăn vào DB
                    using (SqlCommand cmd = new SqlCommand("INSERT INTO ChiTietHoaDon (MaHD, MaSP, SoLuong, DonGia, GhiChu) VALUES (@MaHD, @MaSP, @SL, @Gia, @GhiChu)", conn))
                    {
                        cmd.Parameters.AddWithValue("@MaHD", maHD);
                        cmd.Parameters.AddWithValue("@MaSP", item.MaSP);
                        cmd.Parameters.AddWithValue("@SL", item.SoLuong);
                        cmd.Parameters.AddWithValue("@Gia", item.DonGia);
                        cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrEmpty(item.GhiChu) ? DBNull.Value : item.GhiChu);
                        cmd.ExecuteNonQuery();
                    }
                }

                // CỘNG ĐIỂM NẾU LÀ THANH TOÁN & KHÁCH HÀNG TỒN TẠI
                if (req.Action == "pay" && maKH > 0)
                {
                    int diemCong = (int)(tongTien / 10000); // Công thức: 10k = 1 điểm
                    if (diemCong > 0)
                    {
                        new SqlCommand($"UPDATE KhachHang SET DiemTichLuy = ISNULL(DiemTichLuy, 0) + {diemCong} WHERE MaKH = {maKH}", conn).ExecuteNonQuery();
                    }
                }

                new SqlCommand($"UPDATE Ban SET TrangThai = N'{trangThaiBan}' WHERE MaBan = {req.MaBan}", conn).ExecuteNonQuery();
                string logAction = req.Action == "pay" ? "Thanh toán hóa đơn" : "Lưu món ăn";
                new SqlCommand($"INSERT INTO LichSuThaoTac (MaHD, TenNhanVien, HanhDong) VALUES ({maHD}, N'{tenThuNgan}', N'{logAction}')", conn).ExecuteNonQuery();

                return Json(new { success = true, message = req.Action == "pay" ? "✅ Thu tiền thành công!" : "✅ Đã lưu món & ghi chú!" });
            }
        }
    }
}