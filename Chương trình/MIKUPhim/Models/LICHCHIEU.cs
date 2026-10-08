using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MIKUPhim.Models
{
    public class LICHCHIEU
    {
        [Key]
        public int MaLich { get; set; }

        public int MaPhim { get; set; }

        public DateTime NgayBatDau { get; set; }

        public DateTime NgayKetThuc { get; set; }

        public string TrangThai { get; set; }

        [ForeignKey("MaPhim")]
        public PHIM PHIM { get; set; }
    }
}