using System.ComponentModel.DataAnnotations;

namespace MIKUPhim.Models
{
    public class COMBO
    {
        [Key]
        public int MaCombo { get; set; }

        public string TenCombo { get; set; }
        public string MoTa { get; set; }
        public decimal Gia { get; set; }
        public string HinhAnh { get; set; }
    }
}