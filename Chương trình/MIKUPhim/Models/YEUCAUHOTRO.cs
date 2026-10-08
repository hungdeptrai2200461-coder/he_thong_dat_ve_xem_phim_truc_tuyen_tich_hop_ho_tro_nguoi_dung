using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MIKUPhim.Models
{
    /// <summary>
    /// Yêu cầu hỗ trợ do khách hàng gửi khi phần câu hỏi thường gặp
    /// chưa có câu trả lời phù hợp. Quản trị viên tiếp nhận và phản hồi.
    /// </summary>
    [Table("YEUCAUHOTRO")]
    public class YEUCAUHOTRO
    {
        public const string TrangThaiMoi = "Moi";
        public const string TrangThaiDangXuLy = "DangXuLy";
        public const string TrangThaiDaTraLoi = "DaTraLoi";

        [Key]
        public int MaYeuCau { get; set; }

        /// <summary>Để trống nếu khách gửi khi chưa đăng nhập.</summary>
        public int? MaNguoiDung { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        public string HoTen { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập email liên hệ.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(150, ErrorMessage = "Email không được vượt quá 150 ký tự.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự.")]
        public string TieuDe { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập nội dung cần hỗ trợ.")]
        [StringLength(2000, MinimumLength = 10,
            ErrorMessage = "Nội dung phải từ 10 đến 2000 ký tự.")]
        public string NoiDung { get; set; } = "";

        /// <summary>Moi / DangXuLy / DaTraLoi</summary>
        [StringLength(20)]
        public string TrangThai { get; set; } = TrangThaiMoi;

        [StringLength(2000)]
        public string? NoiDungTraLoi { get; set; }

        public DateTime ThoiDiemGui { get; set; }

        public DateTime? ThoiDiemTraLoi { get; set; }

        [ForeignKey(nameof(MaNguoiDung))]
        public virtual NGUOIDUNG? NGUOIDUNG { get; set; }

        /// <summary>Nhãn tiếng Việt của trạng thái, dùng để hiển thị trên giao diện.</summary>
        [NotMapped]
        public string TenTrangThai => TrangThai switch
        {
            TrangThaiMoi => "Mới gửi",
            TrangThaiDangXuLy => "Đang xử lý",
            TrangThaiDaTraLoi => "Đã trả lời",
            _ => TrangThai
        };
    }
}
