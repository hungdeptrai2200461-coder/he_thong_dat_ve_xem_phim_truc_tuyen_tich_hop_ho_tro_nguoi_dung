using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MIKUPhim.Data;
using MIKUPhim.Helpers;
using MIKUPhim.Models;

namespace MIKUPhim.Controllers
{
    /// <summary>
    /// Mục hỗ trợ người dùng: tra cứu câu hỏi thường gặp theo từ khóa
    /// và gửi yêu cầu hỗ trợ khi chưa có câu trả lời phù hợp.
    /// </summary>
    public class HoTroController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HoTroController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= TRANG HỖ TRỢ =================
        public IActionResult Index(string? tuKhoa)
        {
            var truyVan = _context.ChatBot.AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim();

                truyVan = truyVan.Where(c =>
                    c.CauHoi.Contains(tk) || c.TraLoi.Contains(tk));
            }

            var danhSach = truyVan
                .OrderBy(c => c.MaHoiDap)
                .ToList();

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.KhongTimThay = !string.IsNullOrWhiteSpace(tuKhoa) && danhSach.Count == 0;

            // Điền sẵn thông tin nếu người dùng đã đăng nhập
            var maNguoiDung = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            if (maNguoiDung != null)
            {
                var user = _context.NGUOIDUNG.FirstOrDefault(x => x.MaNguoiDung == maNguoiDung);

                if (user != null)
                {
                    ViewBag.HoTenMacDinh = user.HoTen;
                    ViewBag.EmailMacDinh = user.Email;
                }
            }

            return View(danhSach);
        }

        // ================= GỬI YÊU CẦU HỖ TRỢ =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GuiYeuCau(YEUCAUHOTRO yeuCau)
        {
            // Các trường do hệ thống tự gán, không nhận từ biểu mẫu
            ModelState.Remove(nameof(YEUCAUHOTRO.TrangThai));
            ModelState.Remove(nameof(YEUCAUHOTRO.NoiDungTraLoi));

            if (!ModelState.IsValid)
            {
                // Gom lỗi để hiển thị lại trên trang hỗ trợ
                TempData["LoiYeuCau"] = string.Join(" ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                TempData["HoTenCu"] = yeuCau.HoTen;
                TempData["EmailCu"] = yeuCau.Email;
                TempData["TieuDeCu"] = yeuCau.TieuDe;
                TempData["NoiDungCu"] = yeuCau.NoiDung;

                return RedirectToAction("Index");
            }

            yeuCau.MaNguoiDung = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);
            yeuCau.TrangThai = YEUCAUHOTRO.TrangThaiMoi;
            yeuCau.NoiDungTraLoi = null;
            yeuCau.ThoiDiemGui = DateTime.Now;
            yeuCau.ThoiDiemTraLoi = null;

            _context.YEUCAUHOTRO.Add(yeuCau);
            _context.SaveChanges();

            TempData["Success"] =
                $"Đã gửi yêu cầu hỗ trợ (mã #{yeuCau.MaYeuCau}). "
                + "Quản trị viên sẽ phản hồi trong thời gian sớm nhất.";

            return RedirectToAction("Index");
        }

        // ================= YÊU CẦU CỦA TÔI =================
        [DangNhap]
        public IActionResult YeuCauCuaToi()
        {
            var maNguoiDung = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            var danhSach = _context.YEUCAUHOTRO
                .Where(y => y.MaNguoiDung == maNguoiDung)
                .OrderByDescending(y => y.ThoiDiemGui)
                .ToList();

            return View(danhSach);
        }
    }
}
