namespace KoiCafe.Models
{
    public class CreateProductRequest
    {
        public string TenSP { get; set; }
        public string MoTa { get; set; }
        public decimal DonGia { get; set; }
        public int MaDM { get; set; } 
        public string HinhAnh { get; set; }
    }

    // THÊM MỚI CLASS NÀY ĐỂ HỨNG DỮ LIỆU SỬA
    public class UpdateProductRequest : CreateProductRequest
    {
        public int MaSP { get; set; }
        public bool TrangThai { get; set; } // Cho phép đổi trạng thái Đang bán/Ngừng bán
    }
}