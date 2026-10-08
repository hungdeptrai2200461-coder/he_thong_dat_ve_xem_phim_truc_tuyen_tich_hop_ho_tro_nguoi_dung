using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MIKUPhim.Models
{
    [Table("DONHANG")]
    public class DONHANG
    {
        [Key]
        public int MaDonHang { get; set; }

        [Required]
        public int MaNguoiDung { get; set; }

        [Required]
        public int MaSuatChieu { get; set; }

        [Required]
        public decimal TongTien { get; set; }

        [Required]
        public string TrangThai { get; set; }

        public DateTime ThoiDiem { get; set; }

        // 🔥 FK → NGUOIDUNG
        [ForeignKey(nameof(MaNguoiDung))]
        public virtual NGUOIDUNG NGUOIDUNG { get; set; }

        // 🔥 FK → SUATCHIEU
        [ForeignKey(nameof(MaSuatChieu))]
        public virtual SUATCHIEU SUATCHIEU { get; set; }

        // 🔥 QUAN TRỌNG NHẤT (FIX LỖI CỦA BẠN)
        public virtual ICollection<CT_DONHANG_COMBO> CT_DONHANG_COMBO { get; set; }

        // 🔥 (OPTION) nếu bạn có liên kết với VE
        public virtual ICollection<VE> VE { get; set; }
    }
}