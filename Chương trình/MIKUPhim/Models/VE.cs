using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MIKUPhim.Models
{
    public class VE
    {
        [Key]
        public int MaVe { get; set; }

        public int MaNguoiDung { get; set; }

        public int MaSuatChieu { get; set; }

        public int MaGhe { get; set; }

        public int MaDonHang { get; set; }

        public int TrangThai { get; set; }

        public DateTime ThoiDiemDat { get; set; }

        [ForeignKey("MaDonHang")]
        public DONHANG DONHANG { get; set; }

        [ForeignKey("MaSuatChieu")]
        public SUATCHIEU SUATCHIEU { get; set; }

        [ForeignKey("MaGhe")]
        public GHE GHE { get; set; }
        public virtual ICollection<VE_COMBO> VE_COMBO { get; set; }
    }
}