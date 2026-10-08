namespace MIKUPhim.Helpers
{
    /// <summary>
    /// Kiểm tra và lưu tệp ảnh tải lên.
    /// Chỉ chấp nhận đúng các định dạng ảnh cho phép và giới hạn dung lượng,
    /// tránh việc tải lên tệp thực thi.
    /// </summary>
    public static class UploadHelper
    {
        private static readonly string[] PhanMoRongChoPhep =
            { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        private static readonly string[] KieuNoiDungChoPhep =
            { "image/jpeg", "image/png", "image/gif", "image/webp" };

        public const long DungLuongToiDa = 5 * 1024 * 1024; // 5 MB

        /// <summary>
        /// Kiểm tra tệp ảnh hợp lệ. Trả về null nếu hợp lệ, ngược lại trả về thông báo lỗi.
        /// </summary>
        public static string? KiemTraAnh(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null; // Không tải lên thì bỏ qua, không coi là lỗi

            if (file.Length > DungLuongToiDa)
                return "Ảnh vượt quá dung lượng cho phép (tối đa 5 MB).";

            string phanMoRong = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!PhanMoRongChoPhep.Contains(phanMoRong))
                return "Chỉ chấp nhận ảnh có định dạng JPG, JPEG, PNG, GIF hoặc WEBP.";

            if (!KieuNoiDungChoPhep.Contains(file.ContentType.ToLowerInvariant()))
                return "Tệp tải lên không phải là ảnh hợp lệ.";

            return null;
        }

        /// <summary>
        /// Lưu ảnh vào thư mục con của wwwroot và trả về tên tệp đã sinh.
        /// Trả về null nếu không có tệp nào được tải lên.
        /// Gọi <see cref="KiemTraAnh"/> trước để bảo đảm tệp hợp lệ.
        /// </summary>
        public static string? LuuAnh(IFormFile? file, string thuMucCon)
        {
            if (file == null || file.Length == 0)
                return null;

            string tenTep = Guid.NewGuid().ToString()
                            + Path.GetExtension(file.FileName).ToLowerInvariant();

            string thuMuc = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", thuMucCon);

            Directory.CreateDirectory(thuMuc);

            string duongDan = Path.Combine(thuMuc, tenTep);

            using (var stream = new FileStream(duongDan, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            return tenTep;
        }
    }
}
