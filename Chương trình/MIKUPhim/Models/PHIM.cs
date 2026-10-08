using System.ComponentModel.DataAnnotations;

namespace MIKUPhim.Models
{
    public class PHIM
    {
        [Key]
        public int MaPhim { get; set; }
        public string TenPhim { get; set; }
        public int ThoiLuong { get; set; }
        public int DoTuoi { get; set; }
        public string MoTa { get; set; }
        public string Poster { get; set; }
        public string TrailerUrl { get; set; }
        public string TrangThai { get; set; }
        public string DaoDien { get; set; }
        public string DienVien { get; set; }
        public string TheLoai { get; set; }
        public string NgonNgu { get; set; }
    }
}