using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MIKUPhim.Helpers
{
    /// <summary>
    /// Các khóa dùng chung khi lưu thông tin đăng nhập vào Session.
    /// </summary>
    public static class SessionKeys
    {
        public const string MaNguoiDung = "MaNguoiDung";
        public const string HoTen = "HoTen";
        public const string VaiTro = "VaiTro";
        public const string Avatar = "Avatar";

        public const string VaiTroAdmin = "ADMIN";
        public const string VaiTroUser = "USER";
    }

    /// <summary>
    /// Chỉ cho phép người dùng đã đăng nhập truy cập.
    /// Chưa đăng nhập sẽ được chuyển về trang đăng nhập và quay lại đúng trang cũ sau khi đăng nhập.
    /// </summary>
    public class DangNhapAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            if (session.GetInt32(SessionKeys.MaNguoiDung) == null)
            {
                context.Result = new RedirectToActionResult(
                    "Login", "Account",
                    new { returnUrl = LayDuongDanQuayLai(context.HttpContext) });

                return;
            }

            base.OnActionExecuting(context);
        }

        /// <summary>
        /// Chỉ ghi nhớ đường dẫn quay lại với yêu cầu dạng GET.
        /// Với yêu cầu POST, quay lại bằng GET sẽ gây lỗi 405 nên dùng trang
        /// người dùng vừa rời đi (Referer) thay thế.
        /// </summary>
        internal static string? LayDuongDanQuayLai(HttpContext http)
        {
            if (HttpMethods.IsGet(http.Request.Method))
                return http.Request.Path + http.Request.QueryString;

            string? truocDo = http.Request.Headers["Referer"].ToString();

            if (!string.IsNullOrEmpty(truocDo)
                && Uri.TryCreate(truocDo, UriKind.Absolute, out var uri)
                && string.Equals(uri.Host, http.Request.Host.Host,
                                 StringComparison.OrdinalIgnoreCase))
            {
                return uri.PathAndQuery;
            }

            return null;
        }
    }

    /// <summary>
    /// Chỉ cho phép tài khoản có vai trò ADMIN truy cập.
    /// - Chưa đăng nhập: chuyển về trang đăng nhập.
    /// - Đã đăng nhập nhưng không phải quản trị viên: chuyển sang trang từ chối truy cập.
    /// </summary>
    public class QuanTriVienAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            if (session.GetInt32(SessionKeys.MaNguoiDung) == null)
            {
                context.Result = new RedirectToActionResult(
                    "Login", "Account",
                    new { returnUrl = DangNhapAttribute.LayDuongDanQuayLai(context.HttpContext) });

                return;
            }

            string vaiTro = session.GetString(SessionKeys.VaiTro) ?? "";

            if (!string.Equals(vaiTro, SessionKeys.VaiTroAdmin, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToActionResult(
                    "TuChoiTruyCap", "Account", null);

                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
