using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MIKUPhim.Models
{
    [Table("CT_DONHANG_COMBO")]
    public class CT_DONHANG_COMBO
    {
        [Key]
        public int MaCT { get; set; }

        [Required]
        public int MaDonHang { get; set; }

        [Required]
        public int MaCombo { get; set; }

        [Required]
        public int SoLuong { get; set; }

        // 🔥 Navigation tới DONHANG
        [ForeignKey(nameof(MaDonHang))]
        public virtual DONHANG DONHANG { get; set; }

        // 🔥 Navigation tới COMBO
        [ForeignKey(nameof(MaCombo))]
        public virtual COMBO COMBO { get; set; }
    }
}