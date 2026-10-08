using System;
using System.ComponentModel.DataAnnotations;

namespace MIKUPhim.Models
{
    public class KHUYENMAI
    {
        [Key]
        public int MaKhuyenMai { get; set; }

        public string TieuDe { get; set; }

        public string MoTa { get; set; }

        public string HinhAnh { get; set; }

        public DateTime NgayBatDau { get; set; }

        public DateTime NgayKetThuc { get; set; }
    }
}