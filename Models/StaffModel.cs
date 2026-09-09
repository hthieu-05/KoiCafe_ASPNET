namespace KoiCafe.Models
{
    public class NhanVienModel
    {
        public int MaNV { get; set; }
        public string TenNV { get; set; }
        public string SoDienThoai { get; set; }
        public string VaiTro { get; set; }
        public string TenDangNhap { get; set; } // Ghép từ bảng TaiKhoan
    }

    public class CreateStaffRequest
    {
        public string TenNV { get; set; }
        public string SoDienThoai { get; set; }
        public string VaiTro { get; set; }
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
    }
}