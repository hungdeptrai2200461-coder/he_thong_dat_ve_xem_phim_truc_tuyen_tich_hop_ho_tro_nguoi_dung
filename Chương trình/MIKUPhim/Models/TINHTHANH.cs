using System.ComponentModel.DataAnnotations;

namespace MIKUPhim.Models
{
    public class TINHTHANH
    {
        [Key]
        public int MaTinh { get; set; }

        public string TenTinh { get; set; }
    }
}