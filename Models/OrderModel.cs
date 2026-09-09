namespace KoiCafe.Models
{
    // Model dùng để hiển thị danh sách hóa đơn
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
}