using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MIKUPhim.Models
{
    public class PHONGCHIEU
    {
        [Key]   // 🔥 BẮT BUỘC PHẢI CÓ
        public int MaPhong { get; set; }

        public string TenPhong { get; set; }

        public int MaRap { get; set; }
        public int TongSoGhe { get; set; }

        [ForeignKey("MaRap")]
        public RAP RAP { get; set; }
    }
}