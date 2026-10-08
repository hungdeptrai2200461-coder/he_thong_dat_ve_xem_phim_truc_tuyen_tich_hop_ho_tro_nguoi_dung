using System;
using System.ComponentModel.DataAnnotations;

namespace MIKUPhim.Models
{
    public class THANHTOAN
    {
        [Key]
        public int MaThanhToan { get; set; }

        public int MaVe { get; set; }

        public string PhuongThuc { get; set; }

        public decimal SoTien { get; set; }

        public string TrangThai { get; set; }

        public DateTime ThoiDiem { get; set; }
    }
}