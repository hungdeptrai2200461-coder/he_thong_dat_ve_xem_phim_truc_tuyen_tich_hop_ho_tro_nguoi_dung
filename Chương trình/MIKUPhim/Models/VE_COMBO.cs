using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MIKUPhim.Models
{
    [Table("VE_COMBO")]
    public class VE_COMBO
    {
        [Key, Column(Order = 0)]
        public int MaVe { get; set; }

        [Key, Column(Order = 1)]
        public int MaCombo { get; set; }

        public int SoLuong { get; set; }

        // 🔗 Navigation đến VE
        [ForeignKey("MaVe")]
        public virtual VE VE { get; set; }

        // 🔗 Navigation đến COMBO
        [ForeignKey("MaCombo")]
        public virtual COMBO COMBO { get; set; }
    }
}