using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MIKUPhim.Models
{
    public class SUATCHIEU
    {
        [Key]
        public int MaSuatChieu { get; set; }

        public int MaPhim { get; set; }

        public int MaPhong { get; set; }

        public DateTime NgayChieu { get; set; }

        public TimeSpan GioBatDau { get; set; }

        public TimeSpan GioKetThuc { get; set; }

        public decimal GiaVe { get; set; }

        // 🔥 QUAN TRỌNG
        [ForeignKey("MaPhim")]
        public PHIM PHIM { get; set; }

        [ForeignKey("MaPhong")]
        public PHONGCHIEU PHONGCHIEU { get; set; }
        public virtual ICollection<VE> VE { get; set; }
    }
}