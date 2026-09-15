namespace KoiCafe.Models
{
    public class HoaDonModel
    {
        public int MaHD { get; set; }
<<<<<<< HEAD
        public int MaBan { get; set; } // Đã thêm MaBan chuẩn xác
=======
>>>>>>> 20101f9e67daeb5eca14d71b03f410059b60f3b9
        public string TenBan { get; set; }
        public string TenNV { get; set; }
        public int MaBan { get; set; }
        public DateTime NgayLap { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; }
        public string LyDoHuy { get; set; }
    }

<<<<<<< HEAD
=======
    // THÊM CLASS NÀY ĐỂ CHỨA CHI TIẾT CÁC MÓN TRONG HÓA ĐƠN
>>>>>>> 20101f9e67daeb5eca14d71b03f410059b60f3b9
    public class ChiTietHoaDonModel
    {
        public string TenSP { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
<<<<<<< HEAD
        public decimal ThanhTien => SoLuong * DonGia;
    }

    public class InvoiceProcessRequest
    {
        public int MaHD { get; set; }
        public int MaBan { get; set; }
        public decimal SoTienKhuyenMai { get; set; }
        public string HanhDong { get; set; }
        public string LyDoHuy { get; set; } 
=======
        public decimal ThanhTien => SoLuong * DonGia; // Tự động tính thành tiền
>>>>>>> 20101f9e67daeb5eca14d71b03f410059b60f3b9
    }
    public class InvoiceProcessRequest
    {
        public int MaHD { get; set; }
        public int MaBan { get; set; }
        public decimal SoTienKhuyenMai { get; set; }
        public string HanhDong { get; set; } // "ThanhToan", "Huy", "LuuTam"
        public string LyDoHuy { get; set; } 
    }
}