using System.ComponentModel.DataAnnotations;

namespace MIKUPhim.Models
{
    public class GHE
    {
        [Key]
        public int MaGhe { get; set; }

        public int MaPhong { get; set; }

        public string MaSoGhe { get; set; }

        public string DayGhe { get; set; }
        public int SoGhe { get; set; }

        public string LoaiGhe { get; set; }
        public decimal PhuThu { get; set; }

    }
}