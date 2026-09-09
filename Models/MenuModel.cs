using System.ComponentModel.DataAnnotations;

namespace KoiCafe.Models
{
    public class CreateProductRequest
    {
        public string TenSP { get; set; }
        public string MoTa { get; set; }
        public decimal DonGia { get; set; }
        
        // Thay thế string LoaiSP bằng int MaDM
        public int MaDM { get; set; } 
        
        public string HinhAnh { get; set; }
    }
}