namespace KoiCafe.Models
{
    public class HoaDonModel
    {
        public int MaHD { get; set; }
        public int MaBan { get; set; } // Đã thêm MaBan chuẩn xác
        public string TenBan { get; set; }
        public string TenNV { get; set; }
        public DateTime NgayLap { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; }
        public string LyDoHuy { get; set; }
    }

    public class ChiTietHoaDonModel
    {
        public string TenSP { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;
    }

    public class InvoiceProcessRequest
    {
        public int MaHD { get; set; }
        public int MaBan { get; set; }
        public decimal SoTienKhuyenMai { get; set; }
        public string HanhDong { get; set; }
        public string LyDoHuy { get; set; } 
    }
}