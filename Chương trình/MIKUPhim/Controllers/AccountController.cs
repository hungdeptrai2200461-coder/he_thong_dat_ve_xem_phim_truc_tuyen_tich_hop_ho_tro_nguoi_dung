using Microsoft.AspNetCore.Mvc;
using MIKUPhim.Models;
using MIKUPhim.Data;
using MIKUPhim.Helpers;
using Microsoft.EntityFrameworkCore;

namespace MIKUPhim.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= REGISTER =================
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(NGUOIDUNG user, string XacNhanMatKhau, IFormFile? avatarFile)
        {
            // Hai trường này do hệ thống tự gán, không nhận từ biểu mẫu
            ModelState.Remove(nameof(NGUOIDUNG.VaiTro));
            ModelState.Remove(nameof(NGUOIDUNG.Avatar));

            // Kiểm tra độ mạnh mật khẩu
            string? loiMatKhau = MatKhauHelper.KiemTraDoManh(user.MatKhau);
            if (loiMatKhau != null)
                ModelState.AddModelError(nameof(NGUOIDUNG.MatKhau), loiMatKhau);

            // Kiểm tra xác nhận mật khẩu
            if (user.MatKhau != XacNhanMatKhau)
                ModelState.AddModelError("XacNhanMatKhau", "Mật khẩu xác nhận không khớp.");

            // Kiểm tra email trùng
            if (!string.IsNullOrWhiteSpace(user.Email)
                && _context.NGUOIDUNG.Any(x => x.Email == user.Email))
            {
                ModelState.AddModelError(nameof(NGUOIDUNG.Email), "Email đã được sử dụng.");
            }

            // Kiểm tra tệp ảnh đại diện
            string? loiAnh = UploadHelper.KiemTraAnh(avatarFile);
            if (loiAnh != null)
                ModelState.AddModelError("avatarFile", loiAnh);

            if (!ModelState.IsValid)
                return View(user);

            user.VaiTro = SessionKeys.VaiTroUser;
            user.NgayTao = DateTime.Now;
            user.MatKhau = MatKhauHelper.BamMatKhau(user.MatKhau);
            user.Avatar = UploadHelper.LuuAnh(avatarFile, "images/avatars") ?? "default.png";

            _context.NGUOIDUNG.Add(user);
            _context.SaveChanges();

            TempData["Success"] = "Đăng ký thành công! Mời bạn đăng nhập.";
            return RedirectToAction("Login");
        }

        // ================= LOGIN =================
        public IActionResult Login(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string matKhau, string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(matKhau))
            {
                ViewBag.Loi = "Vui lòng nhập đầy đủ email và mật khẩu!";
                return View();
            }

            var user = _context.NGUOIDUNG.FirstOrDefault(u => u.Email == email);

            if (user == null || !MatKhauHelper.KiemTraMatKhau(matKhau, user.MatKhau))
            {
                ViewBag.Loi = "Sai email hoặc mật khẩu!";
                return View();
            }

            // Tài khoản cũ còn lưu mật khẩu dạng rõ thì nâng cấp sang dạng băm ngay
            if (!MatKhauHelper.LaChuoiDaBam(user.MatKhau))
            {
                user.MatKhau = MatKhauHelper.BamMatKhau(matKhau);
                _context.SaveChanges();
            }

            string vaiTro = string.IsNullOrWhiteSpace(user.VaiTro)
                ? SessionKeys.VaiTroUser
                : user.VaiTro.ToUpperInvariant();

            HttpContext.Session.SetInt32(SessionKeys.MaNguoiDung, user.MaNguoiDung);
            HttpContext.Session.SetString(SessionKeys.HoTen, user.HoTen);
            HttpContext.Session.SetString(SessionKeys.VaiTro, vaiTro);
            HttpContext.Session.SetString(SessionKeys.Avatar, user.Avatar ?? "default.png");

            if (vaiTro == SessionKeys.VaiTroAdmin)
                return RedirectToAction("TongQuan", "Admin");

            // Quay lại đúng trang người dùng định vào trước khi bị yêu cầu đăng nhập
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // ================= LOGOUT =================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ================= TỪ CHỐI TRUY CẬP =================
        public IActionResult TuChoiTruyCap()
        {
            return View();
        }

        // ================= PROFILE =================
        [DangNhap]
        public IActionResult ThongTinCaNhan()
        {
            var maNguoiDung = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            var user = _context.NGUOIDUNG
                .FirstOrDefault(x => x.MaNguoiDung == maNguoiDung);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }

            var ve = _context.VE
                .Where(v => v.MaNguoiDung == maNguoiDung)
                .Include(v => v.GHE)
                .Include(v => v.SUATCHIEU)
                .ThenInclude(s => s.PHIM)
                .ToList();

            ViewBag.Ve = ve;

            return View(user);
        }

        // ================= UPDATE PROFILE =================
        [HttpPost]
        [DangNhap]
        [ValidateAntiForgeryToken]
        public IActionResult CapNhatThongTin(
            string HoTen,
            string Email,
            string SoDienThoai,
            IFormFile? Avatar)
        {
            var id = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            var user = _context.NGUOIDUNG.FirstOrDefault(x => x.MaNguoiDung == id);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }

            // --- Kiểm tra dữ liệu nhập ---
            if (string.IsNullOrWhiteSpace(HoTen))
            {
                TempData["Error"] = "Họ tên không được để trống!";
                return RedirectToAction("ThongTinCaNhan");
            }

            if (string.IsNullOrWhiteSpace(Email)
                || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(Email))
            {
                TempData["Error"] = "Email không đúng định dạng!";
                return RedirectToAction("ThongTinCaNhan");
            }

            // Email mới không được trùng tài khoản khác
            bool emailTrung = _context.NGUOIDUNG
                .Any(x => x.Email == Email && x.MaNguoiDung != user.MaNguoiDung);

            if (emailTrung)
            {
                TempData["Error"] = "Email này đã được tài khoản khác sử dụng!";
                return RedirectToAction("ThongTinCaNhan");
            }

            if (!string.IsNullOrWhiteSpace(SoDienThoai)
                && !System.Text.RegularExpressions.Regex.IsMatch(SoDienThoai, @"^0\d{9,10}$"))
            {
                TempData["Error"] = "Số điện thoại phải gồm 10 hoặc 11 chữ số và bắt đầu bằng số 0!";
                return RedirectToAction("ThongTinCaNhan");
            }

            string? loiAnh = UploadHelper.KiemTraAnh(Avatar);
            if (loiAnh != null)
            {
                TempData["Error"] = loiAnh;
                return RedirectToAction("ThongTinCaNhan");
            }

            // --- Cập nhật ---
            user.HoTen = HoTen;
            user.Email = Email;
            user.SoDienThoai = SoDienThoai;

            string? tenAnh = UploadHelper.LuuAnh(Avatar, "images/avatars");
            if (tenAnh != null)
            {
                user.Avatar = tenAnh;
                HttpContext.Session.SetString(SessionKeys.Avatar, tenAnh);
            }

            _context.SaveChanges();

            HttpContext.Session.SetString(SessionKeys.HoTen, user.HoTen);

            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("ThongTinCaNhan");
        }

        // ================= CHANGE PASSWORD =================
        [DangNhap]
        public IActionResult DoiMatKhau()
        {
            // Biểu mẫu đổi mật khẩu nằm trong trang thông tin cá nhân
            return RedirectToAction("ThongTinCaNhan");
        }

        [HttpPost]
        [DangNhap]
        [ValidateAntiForgeryToken]
        public IActionResult DoiMatKhau(string matKhauCu, string matKhauMoi, string xacNhanMatKhauMoi)
        {
            var id = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            var user = _context.NGUOIDUNG.FirstOrDefault(x => x.MaNguoiDung == id);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }

            if (!MatKhauHelper.KiemTraMatKhau(matKhauCu, user.MatKhau))
            {
                TempData["Error"] = "Mật khẩu cũ không đúng!";
                return RedirectToAction("ThongTinCaNhan");
            }

            string? loiMatKhau = MatKhauHelper.KiemTraDoManh(matKhauMoi);
            if (loiMatKhau != null)
            {
                TempData["Error"] = loiMatKhau;
                return RedirectToAction("ThongTinCaNhan");
            }

            if (matKhauMoi != xacNhanMatKhauMoi)
            {
                TempData["Error"] = "Mật khẩu xác nhận không khớp!";
                return RedirectToAction("ThongTinCaNhan");
            }

            if (matKhauCu == matKhauMoi)
            {
                TempData["Error"] = "Mật khẩu mới phải khác mật khẩu cũ!";
                return RedirectToAction("ThongTinCaNhan");
            }

            user.MatKhau = MatKhauHelper.BamMatKhau(matKhauMoi);

            _context.SaveChanges();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("ThongTinCaNhan");
        }

        // ================= LỊCH SỬ VÉ =================
        [DangNhap]
        public IActionResult LichSuVe()
        {
            var maNguoiDung = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            DateTime homNay = DateTime.Today;
            TimeSpan gioHienTai = DateTime.Now.TimeOfDay;

            var ves = _context.VE
                .Include(v => v.GHE)
                .Include(v => v.SUATCHIEU)
                    .ThenInclude(s => s.PHIM)
                .Include(v => v.SUATCHIEU)
                    .ThenInclude(s => s.PHONGCHIEU)
                        .ThenInclude(p => p.RAP)
                .Where(v =>
                    v.MaNguoiDung == maNguoiDung &&
                    v.TrangThai == 1 &&
                    (
                        v.SUATCHIEU.NgayChieu < homNay ||
                        (v.SUATCHIEU.NgayChieu == homNay &&
                         v.SUATCHIEU.GioKetThuc <= gioHienTai)
                    )
                )
                .OrderByDescending(v => v.ThoiDiemDat)
                .ToList();

            ViewBag.TongChiTieu = ves.Sum(v => v.SUATCHIEU.GiaVe);

            return View(ves);
        }
    }
}
