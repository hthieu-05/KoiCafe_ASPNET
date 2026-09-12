namespace KoiCafe.Models
{
    public class HoaDonModel
    {
        public int MaHD { get; set; }
        public string TenBan { get; set; }
        public string TenNV { get; set; }
        public DateTime NgayLap { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; }
        public string LyDoHuy { get; set; }
    }

    // THÊM CLASS NÀY ĐỂ CHỨA CHI TIẾT CÁC MÓN TRONG HÓA ĐƠN
    public class ChiTietHoaDonModel
    {
        public string TenSP { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien => SoLuong * DonGia; // Tự động tính thành tiền
    }
}