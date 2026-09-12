namespace KoiCafe.Models
{
    // Model dùng để hiển thị Bàn
    public class BanModel
    {
        public int MaBan { get; set; }
        public string TenBan { get; set; }
        public string KhuVuc { get; set; }
        public int SucChua { get; set; }
        public string TrangThai { get; set; }
    }

    // Model dùng để hiển thị Đặt Bàn
    public class DatBanModel
    {
        public int MaDatBan { get; set; }
        public string TenKhachHang { get; set; }
        public string SoDienThoai { get; set; }
        public DateTime NgayDat { get; set; }
        public TimeSpan GioDat { get; set; }
        public int SoNguoi { get; set; }
        public string GhiChu { get; set; }
        public string TrangThai { get; set; }
        public int? MaBan { get; set; }
        // Dữ liệu nối từ bảng Ban
        public string TenBan { get; set; } 
        public string KhuVuc { get; set; }
    }

    // Gói dữ liệu (ViewModel) gửi ra View của trang Bookings
    public class BookingViewModel
    {
        public List<DatBanModel> Bookings { get; set; } = new List<DatBanModel>();
        public List<BanModel> Tables { get; set; } = new List<BanModel>();
    }

    // Hứng dữ liệu AJAX khi ấn Nút Cập nhật trạng thái
    public class UpdateStatusRequest
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public int? MaBan { get; set; }
    }
}