using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MIKUPhim.Models
{
    [Table("NGUOIDUNG")]
    [Index(nameof(Email), IsUnique = true)]
    public class NGUOIDUNG
    {
        [Key]
        public int MaNguoiDung { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Họ tên phải từ 2 đến 100 ký tự.")]
        [Display(Name = "Họ tên")]
        public string HoTen { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(150, ErrorMessage = "Email không được vượt quá 150 ký tự.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(200, MinimumLength = 6,
            ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^0\d{9,10}$",
            ErrorMessage = "Số điện thoại phải gồm 10 hoặc 11 chữ số và bắt đầu bằng số 0.")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; } = "";

        public string? Avatar { get; set; }

        [Display(Name = "Vai trò")]
        public string VaiTro { get; set; } = "USER";

        public DateTime NgayTao { get; set; }
    }
}
