using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MIKUPhim.Models
{
    /// <summary>
    /// Câu hỏi thường gặp và câu trả lời được thiết lập sẵn.
    /// Quản trị viên quản lý nội dung này, khách hàng tra cứu theo từ khóa.
    /// </summary>
    [Table("CHATBOT")] // Đảm bảo khớp với tên bảng viết hoa trong SQL
    public class ChatBot
    {
        [Key]
        public int MaHoiDap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập câu hỏi hoặc từ khóa.")]
        [StringLength(500, ErrorMessage = "Câu hỏi không được vượt quá 500 ký tự.")]
        [Display(Name = "Câu hỏi / từ khóa")]
        public string CauHoi { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập câu trả lời.")]
        [StringLength(2000, ErrorMessage = "Câu trả lời không được vượt quá 2000 ký tự.")]
        [Display(Name = "Câu trả lời")]
        public string TraLoi { get; set; } = "";
    }
}
