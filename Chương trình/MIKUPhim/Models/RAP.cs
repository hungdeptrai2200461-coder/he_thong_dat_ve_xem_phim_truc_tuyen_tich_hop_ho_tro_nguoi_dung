using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MIKUPhim.Models
{
    public class RAP
    {
        [Key]
        public int MaRap { get; set; }

        public string TenRap { get; set; }

        public int MaTinh { get; set; }

        public string DiaChi { get; set; }

        [ForeignKey("MaTinh")]
        public TINHTHANH TINHTHANH { get; set; }
    }
}