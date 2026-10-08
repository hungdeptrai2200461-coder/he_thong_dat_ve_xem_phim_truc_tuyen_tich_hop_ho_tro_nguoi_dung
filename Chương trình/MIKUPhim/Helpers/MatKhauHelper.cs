using System.Text.RegularExpressions;

namespace MIKUPhim.Helpers
{
    /// <summary>
    /// Xử lý băm và kiểm tra mật khẩu bằng thuật toán BCrypt.
    /// Mật khẩu không bao giờ được lưu dạng rõ trong cơ sở dữ liệu.
    /// </summary>
    public static class MatKhauHelper
    {
        // Chuỗi BCrypt luôn có dạng $2a$ / $2b$ / $2y$ + độ khó + salt + hash (60 ký tự)
        private static readonly Regex BCryptPattern =
            new Regex(@"^\$2[aby]\$\d{2}\$[./A-Za-z0-9]{53}$", RegexOptions.Compiled);

        /// <summary>Băm mật khẩu dạng rõ thành chuỗi BCrypt để lưu vào cơ sở dữ liệu.</summary>
        public static string BamMatKhau(string matKhauRo)
        {
            if (string.IsNullOrEmpty(matKhauRo))
                throw new ArgumentException("Mật khẩu không được để trống.", nameof(matKhauRo));

            return BCrypt.Net.BCrypt.HashPassword(matKhauRo, workFactor: 11);
        }

        /// <summary>Kiểm tra một chuỗi đã ở dạng băm BCrypt hay chưa.</summary>
        public static bool LaChuoiDaBam(string? giaTri)
        {
            return !string.IsNullOrEmpty(giaTri) && BCryptPattern.IsMatch(giaTri);
        }

        /// <summary>
        /// Đối chiếu mật khẩu người dùng nhập với giá trị đang lưu.
        /// Nếu giá trị đang lưu còn ở dạng rõ (dữ liệu cũ) thì so sánh trực tiếp,
        /// để tài khoản cũ vẫn đăng nhập được và được nâng cấp sang dạng băm ngay sau đó.
        /// </summary>
        public static bool KiemTraMatKhau(string matKhauNhap, string? matKhauLuu)
        {
            if (string.IsNullOrEmpty(matKhauNhap) || string.IsNullOrEmpty(matKhauLuu))
                return false;

            if (LaChuoiDaBam(matKhauLuu))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(matKhauNhap, matKhauLuu);
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    return false;
                }
            }

            // Dữ liệu cũ chưa băm
            return matKhauNhap == matKhauLuu;
        }

        /// <summary>
        /// Kiểm tra độ mạnh tối thiểu của mật khẩu.
        /// Trả về null nếu hợp lệ, ngược lại trả về thông báo lỗi.
        /// </summary>
        public static string? KiemTraDoManh(string? matKhau)
        {
            if (string.IsNullOrWhiteSpace(matKhau))
                return "Mật khẩu không được để trống.";

            if (matKhau.Length < 6)
                return "Mật khẩu phải có ít nhất 6 ký tự.";

            if (matKhau.Length > 100)
                return "Mật khẩu không được vượt quá 100 ký tự.";

            if (!matKhau.Any(char.IsLetter) || !matKhau.Any(char.IsDigit))
                return "Mật khẩu phải chứa cả chữ và số.";

            return null;
        }
    }
}
