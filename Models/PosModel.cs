namespace KoiCafe.Models
{
    // Dữ liệu truyền ra màn hình POS
    public class PosViewModel
    {
        public List<BanModel> Tables { get; set; } = new List<BanModel>();
        public List<SanPhamModel> Products { get; set; } = new List<SanPhamModel>();
    }

    public class SanPhamModel
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public decimal DonGia { get; set; }
        public string HinhAnh { get; set; }
    }

    // Hứng dữ liệu JSON từ Javascript (khi ấn Lưu/Thanh toán)
    public class PosRequest
    {
        public int MaBan { get; set; }
        public string Action { get; set; } // 'save', 'pay', 'cancel'
        public string SdtKhach { get; set; }
        public string TenKhach { get; set; }
        public string LyDoHuy { get; set; }
        public List<CartItem> Cart { get; set; }
    }

    public class CartItem
    {
        public int MaSP { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
    }
}