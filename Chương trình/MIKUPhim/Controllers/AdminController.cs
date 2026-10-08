using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MIKUPhim.Data;
using MIKUPhim.Helpers;
using MIKUPhim.Models;
using System;
using System.Linq;

namespace MIKUPhim.Controllers
{
    /// <summary>
    /// Khu vực quản trị. Thuộc tính [QuanTriVien] áp ở cấp lớp nên mọi chức năng
    /// bên trong đều yêu cầu tài khoản có vai trò ADMIN.
    /// </summary>
    [QuanTriVien]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult TongQuan()
        {
            var homNay = DateTime.Today;

            // Doanh thu hôm nay
            var doanhThuHomNay = _context.DONHANG
                .Where(d => d.ThoiDiem.Date == homNay)
                .Sum(d => (decimal?)d.TongTien) ?? 0;

            // Tổng vé bán hôm nay
            var tongVe = _context.VE
                .Where(v => v.ThoiDiemDat.Date == homNay)
                .Count();

            // Khách hàng mới hôm nay
            var khachMoi = _context.NGUOIDUNG
                .Where(u => u.NgayTao.Date == homNay)
                .Count();

            // Tổng doanh thu toàn hệ thống
            var tongDoanhThu = _context.DONHANG
                .Sum(d => (decimal?)d.TongTien) ?? 0;

            ViewBag.DoanhThuHomNay = doanhThuHomNay;
            ViewBag.TongVe = tongVe;
            ViewBag.KhachMoi = khachMoi;
            ViewBag.TongDoanhThu = tongDoanhThu;


            // ===============================
            // Doanh thu theo tháng (Chart)
            // ===============================
            var doanhThuThang = _context.DONHANG
                .GroupBy(d => d.ThoiDiem.Month)
                .Select(g => new
                {
                    Thang = g.Key,
                    Tong = g.Sum(x => x.TongTien)
                })
                .OrderBy(x => x.Thang)
                .ToList();

            ViewBag.Thang = doanhThuThang.Select(x => x.Thang).ToArray();
            ViewBag.DoanhThuThang = doanhThuThang.Select(x => x.Tong).ToArray();


            // ===============================
            // Vé bán theo phim (Bar Chart)
            // ===============================
            var veTheoPhim = _context.VE
                .Join(_context.SUATCHIEU,
                    v => v.MaSuatChieu,
                    s => s.MaSuatChieu,
                    (v, s) => new { v, s })
                .Join(_context.PHIM,
                    vs => vs.s.MaPhim,
                    p => p.MaPhim,
                    (vs, p) => new
                    {
                        TenPhim = p.TenPhim
                    })
                .GroupBy(x => x.TenPhim)
                .Select(g => new
                {
                    TenPhim = g.Key,
                    SoVe = g.Count()
                })
                .ToList();

            ViewBag.TenPhim = veTheoPhim.Select(x => x.TenPhim).ToArray();
            ViewBag.SoVePhim = veTheoPhim.Select(x => x.SoVe).ToArray();
            var doanhThuPhim = _context.VE
                .Join(_context.SUATCHIEU,
                    v => v.MaSuatChieu,
                    s => s.MaSuatChieu,
                    (v, s) => new { v, s })
                .Join(_context.PHIM,
                    vs => vs.s.MaPhim,
                    p => p.MaPhim,
                    (vs, p) => new
                    {
                        TenPhim = p.TenPhim,
                        GiaVe = vs.s.GiaVe
                    })
                .GroupBy(x => x.TenPhim)
                .Select(g => new
                {
                    TenPhim = g.Key,
                    SoVe = g.Count(),
                    DoanhThu = g.Sum(x => x.GiaVe)
                })
                .ToList();

                    ViewBag.DoanhThuPhim = doanhThuPhim;
                    var doanhThuRap = _context.VE
            .Join(_context.SUATCHIEU,
                v => v.MaSuatChieu,
                s => s.MaSuatChieu,
                (v, s) => new { v, s })
            .Join(_context.PHONGCHIEU,
                vs => vs.s.MaPhong,
                pc => pc.MaPhong,
                (vs, pc) => new { vs, pc })
            .Join(_context.RAP,
                vsp => vsp.pc.MaRap,
                r => r.MaRap,
                (vsp, r) => new
                {
                    TenRap = r.TenRap,
                    GiaVe = vsp.vs.s.GiaVe
                })
            .GroupBy(x => x.TenRap)
            .Select(g => new
            {
                TenRap = g.Key,
                SoVe = g.Count(),
                DoanhThu = g.Sum(x => x.GiaVe)
            })
            .ToList();

            ViewBag.DoanhThuRap = doanhThuRap;
            return View();
        }
        public IActionResult DoanhThuPhim(DateTime? tuNgay, DateTime? denNgay)
        {
            if (tuNgay == null)
                tuNgay = DateTime.Today.AddMonths(-1);

            if (denNgay == null)
                denNgay = DateTime.Today;

            var doanhThuPhim = _context.VE
                .Join(_context.SUATCHIEU,
                    v => v.MaSuatChieu,
                    s => s.MaSuatChieu,
                    (v, s) => new { v, s })
                .Join(_context.PHIM,
                    vs => vs.s.MaPhim,
                    p => p.MaPhim,
                    (vs, p) => new
                    {
                        TenPhim = p.TenPhim,
                        GiaVe = vs.s.GiaVe,
                        Ngay = vs.s.NgayChieu
                    })
                .Where(x => x.Ngay >= tuNgay && x.Ngay <= denNgay)
                .GroupBy(x => x.TenPhim)
                .Select(g => new
                {
                    TenPhim = g.Key,
                    SoVe = g.Count(),
                    DoanhThu = g.Sum(x => x.GiaVe)
                })
                .ToList();

            ViewBag.DoanhThuPhim = doanhThuPhim;

            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");

            return View();
        }
        public IActionResult DoanhThuRap(DateTime? tuNgay, DateTime? denNgay)
        {
            if (tuNgay == null)
                tuNgay = DateTime.Today.AddMonths(-1);

            if (denNgay == null)
                denNgay = DateTime.Today;

            var doanhThuRap = _context.VE
                .Join(_context.SUATCHIEU,
                    v => v.MaSuatChieu,
                    s => s.MaSuatChieu,
                    (v, s) => new { v, s })
                .Join(_context.PHONGCHIEU,
                    vs => vs.s.MaPhong,
                    pc => pc.MaPhong,
                    (vs, pc) => new { vs, pc })
                .Join(_context.RAP,
                    vsp => vsp.pc.MaRap,
                    r => r.MaRap,
                    (vsp, r) => new
                    {
                        TenRap = r.TenRap,
                        GiaVe = vsp.vs.s.GiaVe,
                        Ngay = vsp.vs.s.NgayChieu
                    })
                .Where(x => x.Ngay >= tuNgay && x.Ngay <= denNgay)
                .GroupBy(x => x.TenRap)
                .Select(g => new
                {
                    TenRap = g.Key,
                    SoVe = g.Count(),
                    DoanhThu = g.Sum(x => x.GiaVe)
                })
                .ToList();

            ViewBag.DoanhThuRap = doanhThuRap;

            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");

            return View();
        }
        public IActionResult QuanLyPhim(string search, string trangThai, int page = 1)
        {
            int pageSize = 5;

            var phim = _context.PHIM.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                phim = phim.Where(p => p.TenPhim.Contains(search));
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                phim = phim.Where(p => p.TrangThai == trangThai);
            }

            int total = phim.Count();

            var data = phim
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPage = Math.Ceiling((double)total / pageSize);
            ViewBag.CurrentPage = page;

            return View(data);
        }
        public IActionResult ThemPhim()
        {
            return View();
        }
        [HttpPost]
        public IActionResult ThemPhim(PHIM p, IFormFile PosterFile)
        {
            if (PosterFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(PosterFile.FileName);

                string path = Path.Combine(Directory.GetCurrentDirectory(),
                                           "wwwroot/images",
                                           fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    PosterFile.CopyTo(stream);
                }

                p.Poster = "/images/" + fileName;
            }

            _context.PHIM.Add(p);
            _context.SaveChanges();

            return RedirectToAction("QuanLyPhim");
        }
        public IActionResult SuaPhim(int id)
        {
            var phim = _context.PHIM.Find(id);
            return View(phim);
        }
        [HttpPost]
        public IActionResult SuaPhim(PHIM p)
        {
            if (ModelState.IsValid)
            {
                _context.PHIM.Update(p);
                _context.SaveChanges();
                return RedirectToAction("QuanLyPhim");
            }

            return View(p);
        }
        public IActionResult XoaPhim(int id)
        {
            var phim = _context.PHIM.Find(id);

            if (phim != null)
            {
                _context.PHIM.Remove(phim);
                _context.SaveChanges();
            }

            return RedirectToAction("QuanLyPhim");
        }
        public IActionResult QuanLyRap(int page = 1)
        {
            int pageSize = 5;

            var rap = _context.RAP
                .Include(r => r.TINHTHANH)
                .AsQueryable();

            int total = rap.Count();

            var data = rap
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPage = Math.Ceiling((double)total / pageSize);
            ViewBag.CurrentPage = page;

            return View(data);
        }
        public IActionResult ThemRap()
        {
            ViewBag.TinhThanh = _context.TINHTHANH.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult ThemRap(RAP r)
        {
            if (ModelState.IsValid)
            {
                _context.RAP.Add(r);
                _context.SaveChanges();
                return RedirectToAction("QuanLyRap");
            }

            ViewBag.TinhThanh = _context.TINHTHANH.ToList();
            return View(r);
        }
        public IActionResult SuaRap(int id)
        {
            var rap = _context.RAP.Find(id);

            ViewBag.TinhThanh = _context.TINHTHANH.ToList();

            return View(rap);
        }

        [HttpPost]
        public IActionResult SuaRap(RAP r)
        {
            if (ModelState.IsValid)
            {
                _context.RAP.Update(r);
                _context.SaveChanges();
                return RedirectToAction("QuanLyRap");
            }

            ViewBag.TinhThanh = _context.TINHTHANH.ToList();
            return View(r);
        }
        public IActionResult XoaRap(int id)
        {
            var rap = _context.RAP.Find(id);

            if (rap != null)
            {
                _context.RAP.Remove(rap);
                _context.SaveChanges();
            }

            return RedirectToAction("QuanLyRap");
        }
        public IActionResult QuanLyPhong(int page = 1)
        {
            int pageSize = 5;

            var phong = _context.PHONGCHIEU
                .Include(p => p.RAP)
                .AsQueryable();

            int total = phong.Count();

            var data = phong
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPage = Math.Ceiling((double)total / pageSize);
            ViewBag.CurrentPage = page;

            return View(data);
        }
        public IActionResult ThemPhong()
        {
            ViewBag.Rap = _context.RAP.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult ThemPhong(PHONGCHIEU p)
        {
            if (ModelState.IsValid)
            {
                _context.PHONGCHIEU.Add(p);
                _context.SaveChanges();
                return RedirectToAction("QuanLyPhong");
            }

            ViewBag.Rap = _context.RAP.ToList();
            return View(p);
        }
        public IActionResult SuaPhong(int id)
        {
            var phong = _context.PHONGCHIEU.Find(id);

            ViewBag.Rap = _context.RAP.ToList();

            return View(phong);
        }

        [HttpPost]
        public IActionResult SuaPhong(PHONGCHIEU p)
        {
            if (ModelState.IsValid)
            {
                _context.PHONGCHIEU.Update(p);
                _context.SaveChanges();

                return RedirectToAction("QuanLyPhong");
            }

            ViewBag.Rap = _context.RAP.ToList();
            return View(p);
        }
        public IActionResult XoaPhong(int id)
        {
            var phong = _context.PHONGCHIEU.Find(id);

            if (phong != null)
            {
                _context.PHONGCHIEU.Remove(phong);
                _context.SaveChanges();
            }

            return RedirectToAction("QuanLyPhong");
        }
        public IActionResult QuanLyGhe(int maPhong)
        {
            var phong = _context.PHONGCHIEU
                .FirstOrDefault(p => p.MaPhong == maPhong);

            ViewBag.MaPhong = maPhong;

            if (phong.TenPhong.ToLower().Contains("imax"))
            {
                ViewBag.TongGhe = 300;
            }
            else
            {
                ViewBag.TongGhe = 120;
            }

            return View();
        }
        [HttpPost]
        public IActionResult SaveSeats(int maPhong, [FromBody] List<SeatDTO> seats)
        {
            var old = _context.GHE
                .Where(g => g.MaPhong == maPhong)
                .ToList();

            _context.GHE.RemoveRange(old);

            foreach (var s in seats)
            {
                GHE g = new GHE();

                g.MaPhong = maPhong;
                g.MaSoGhe = s.code;
                g.DayGhe = s.code.Substring(0, 1);
                g.SoGhe = int.Parse(s.code.Substring(1));

                if (s.type == "seat-vip")
                {
                    g.LoaiGhe = "VIP";
                    g.PhuThu = 20000;
                }
                else if (s.type == "seat-couple")
                {
                    g.LoaiGhe = "DOI";
                    g.PhuThu = 30000;
                }
                else
                {
                    g.LoaiGhe = "THUONG";
                    g.PhuThu = 0;
                }

                _context.GHE.Add(g);
            }

            _context.SaveChanges();

            return Json(new { success = true });
        }
      
        public IActionResult QuanLyLichChieu(string locTrangThai, int page = 1)
        {
            int pageSize = 10;

            // 🔥 tự động cập nhật trạng thái theo ngày
            var today = DateTime.Today;

            var tatCaLich = _context.LICHCHIEU.ToList();

            foreach (var l in tatCaLich)
            {
                if (today < l.NgayBatDau)
                    l.TrangThai = "SapChieu";

                else if (today >= l.NgayBatDau && today <= l.NgayKetThuc)
                    l.TrangThai = "DangChieu";

                else
                    l.TrangThai = "DaKetThuc";
            }

            _context.SaveChanges();

            // query
            var data = _context.LICHCHIEU
                .Include(x => x.PHIM)
                .OrderByDescending(x => x.NgayBatDau)
                .AsQueryable();

            // 🔥 lọc phim đang chiếu
            if (!string.IsNullOrEmpty(locTrangThai))
            {
                data = data.Where(x => x.TrangThai == locTrangThai);
            }

            int total = data.Count();

            var list = data
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPage = Math.Ceiling((double)total / pageSize);
            ViewBag.CurrentPage = page;

            ViewBag.Phim = _context.PHIM.ToList();
            ViewBag.TrangThai = locTrangThai;

            return View(list);
        }
        [HttpPost]
        public IActionResult ThemLichChieu(LICHCHIEU l)
        {
            // 🔥 không cho ngày kết thúc < ngày bắt đầu
            if (l.NgayKetThuc < l.NgayBatDau)
            {
                TempData["Error"] = "Ngày kết thúc không được nhỏ hơn ngày bắt đầu!";
                return RedirectToAction("QuanLyLichChieu");
            }

            // 🔥 tự động set trạng thái
            var today = DateTime.Today;

            if (today < l.NgayBatDau)
                l.TrangThai = "SapChieu";

            else if (today >= l.NgayBatDau && today <= l.NgayKetThuc)
                l.TrangThai = "DangChieu";

            else
                l.TrangThai = "DaKetThuc";

            _context.LICHCHIEU.Add(l);
            _context.SaveChanges();

            return RedirectToAction("QuanLyLichChieu");
        }
        public IActionResult SuaLichChieu(int id)
        {
            var data = _context.LICHCHIEU.Find(id);

            ViewBag.Phim = _context.PHIM.ToList();

            return View(data);
        }
        [HttpPost]
        public IActionResult SuaLichChieu(LICHCHIEU l)
        {
            // 🔥 kiểm tra ngày
            if (l.NgayKetThuc < l.NgayBatDau)
            {
                TempData["Error"] = "Ngày kết thúc không hợp lệ!";
                return RedirectToAction("QuanLyLichChieu");
            }

            // 🔥 cập nhật trạng thái tự động
            var today = DateTime.Today;

            if (today < l.NgayBatDau)
                l.TrangThai = "SapChieu";

            else if (today >= l.NgayBatDau && today <= l.NgayKetThuc)
                l.TrangThai = "DangChieu";

            else
                l.TrangThai = "DaKetThuc";

            _context.LICHCHIEU.Update(l);
            _context.SaveChanges();

            return RedirectToAction("QuanLyLichChieu");
        }
        public IActionResult XoaLichChieu(int id)
        {
            var data = _context.LICHCHIEU.Find(id);

            _context.LICHCHIEU.Remove(data);

            _context.SaveChanges();

            return RedirectToAction("QuanLyLichChieu");
        }
        public IActionResult QuanLySuatChieu(DateTime? ngay)
        {
            if (ngay == null)
                ngay = DateTime.Today;

            var data = _context.SUATCHIEU
                .Include(x => x.PHIM)
                .Include(x => x.PHONGCHIEU)
                    .ThenInclude(p => p.RAP)
                .Where(x => x.NgayChieu.Date == ngay.Value.Date)
                .OrderBy(x => x.GioBatDau)
                .ToList();

            ViewBag.Ngay = ngay;

            // 🔥 Load dropdown
            ViewBag.Phim = _context.PHIM.ToList();
            ViewBag.Rap = _context.RAP.ToList();

            return View(data);
        }

        // ===============================
        // 🔥 LOAD PHÒNG THEO RẠP (AJAX)
        // ===============================
        public JsonResult GetPhongByRap(int maRap)
        {
            var phong = _context.PHONGCHIEU
                .Where(p => p.MaRap == maRap)
                .Select(p => new
                {
                    maPhong = p.MaPhong,
                    tenPhong = p.TenPhong
                })
                .ToList();

            return Json(phong);
        }

        // ===============================
        // 🔥 LẤY THỜI LƯỢNG PHIM (AJAX)
        // ===============================
        public JsonResult GetThoiLuong(int maPhim)
        {
            var thoiLuong = _context.PHIM
                .Where(p => p.MaPhim == maPhim)
                .Select(p => p.ThoiLuong)
                .FirstOrDefault();

            return Json(thoiLuong);
        }

        // ===============================
        // ➕ THÊM SUẤT CHIẾU
        // ===============================
        [HttpPost]
        public IActionResult ThemSuatChieu(SUATCHIEU s)
        {
            var phim = _context.PHIM.FirstOrDefault(p => p.MaPhim == s.MaPhim);

            if (phim == null)
            {
                TempData["Error"] = "Phim không tồn tại!";
                return RedirectToAction("QuanLySuatChieu");
            }

            // 🔥 AUTO GIỜ KẾT THÚC
            s.GioKetThuc = s.GioBatDau.Add(TimeSpan.FromMinutes(phim.ThoiLuong));

            // 🔥 CHECK TRÙNG GIỜ
            bool trung = _context.SUATCHIEU.Any(x =>
                x.MaPhong == s.MaPhong &&
                x.NgayChieu.Date == s.NgayChieu.Date &&
                (
                    s.GioBatDau < x.GioKetThuc &&
                    s.GioKetThuc > x.GioBatDau
                )
            );

            if (trung)
            {
                TempData["Error"] = "⚠️ Phòng đã có suất chiếu trùng giờ!";
                return RedirectToAction("QuanLySuatChieu", new { ngay = s.NgayChieu });
            }

            _context.SUATCHIEU.Add(s);
            _context.SaveChanges();

            return RedirectToAction("QuanLySuatChieu", new { ngay = s.NgayChieu });
        }

        // ===============================
        // ✏️ SỬA SUẤT CHIẾU
        // ===============================
        public IActionResult SuaSuatChieu(int id)
        {
            var data = _context.SUATCHIEU
                .Include(x => x.PHIM)
                .Include(x => x.PHONGCHIEU)
                    .ThenInclude(p => p.RAP)
                .FirstOrDefault(x => x.MaSuatChieu == id);

            if (data == null)
                return NotFound();

            ViewBag.Phim = _context.PHIM.ToList();
            ViewBag.Rap = _context.RAP.ToList();

            // 🔥 THÊM DÒNG NÀY (QUAN TRỌNG)
            ViewBag.Phong = _context.PHONGCHIEU.ToList();

            return View(data);
        }
        [HttpPost]
        public IActionResult SuaSuatChieu(SUATCHIEU s)
        {
            if (s.GioBatDau >= s.GioKetThuc)
            {
                TempData["Error"] = "Giờ kết thúc phải lớn hơn giờ bắt đầu!";
                return RedirectToAction("QuanLySuatChieu", new { ngay = s.NgayChieu });
            }

            var phim = _context.PHIM.FirstOrDefault(p => p.MaPhim == s.MaPhim);

            if (phim == null)
            {
                TempData["Error"] = "Phim không tồn tại!";
                return RedirectToAction("QuanLySuatChieu");
            }

            // 🔥 AUTO GIỜ KẾT THÚC
            s.GioKetThuc = s.GioBatDau.Add(TimeSpan.FromMinutes(phim.ThoiLuong));

            // 🔥 CHECK TRÙNG
            bool trung = _context.SUATCHIEU.Any(x =>
                x.MaPhong == s.MaPhong &&
                x.NgayChieu.Date == s.NgayChieu.Date &&
                x.MaSuatChieu != s.MaSuatChieu &&
                (
                    s.GioBatDau < x.GioKetThuc &&
                    s.GioKetThuc > x.GioBatDau
                )
            );

            if (trung)
            {
                TempData["Error"] = "⚠️ Khung giờ bị trùng!";
                return RedirectToAction("QuanLySuatChieu", new { ngay = s.NgayChieu });
            }

            _context.SUATCHIEU.Update(s);
            _context.SaveChanges();

            return RedirectToAction("QuanLySuatChieu", new { ngay = s.NgayChieu });
        }

        // ===============================
        // ❌ XOÁ SUẤT CHIẾU
        // ===============================
        public IActionResult XoaSuatChieu(int id)
        {
            var data = _context.SUATCHIEU
                .Include(x => x.VE)
                .FirstOrDefault(x => x.MaSuatChieu == id);

            if (data == null)
                return NotFound();

            // 🔥 CHẶN XOÁ NẾU ĐÃ CÓ VÉ
            if (data.VE != null && data.VE.Any())
            {
                TempData["Error"] = "❌ Không thể xoá suất chiếu đã có vé!";
                return RedirectToAction("QuanLySuatChieu");
            }

            _context.SUATCHIEU.Remove(data);
            _context.SaveChanges();

            return RedirectToAction("QuanLySuatChieu");
        }
    public IActionResult QuanLyDonHang(string? trangThai, int page = 1)
        {
            int pageSize = 10;

            // Không dùng Include cho SUATCHIEU vì Entity Framework sẽ nối bảng kiểu INNER JOIN,
            // khiến các đơn hàng có suất chiếu không còn tồn tại bị loại khỏi danh sách.
            var query = _context.DONHANG.AsQueryable();

            // Lọc theo trạng thái nếu người dùng chọn, mặc định hiển thị tất cả
            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(x => x.TrangThai == trangThai);
            }

            query = query.OrderByDescending(x => x.ThoiDiem);

            int total = query.Count();

            var data = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Nạp dữ liệu liên quan riêng để đơn hàng thiếu suất chiếu vẫn hiển thị được
            var dsMaNguoiDung = data.Select(d => d.MaNguoiDung).Distinct().ToList();
            var dsMaSuatChieu = data.Select(d => d.MaSuatChieu).Distinct().ToList();

            var nguoiDungs = _context.NGUOIDUNG
                .Where(n => dsMaNguoiDung.Contains(n.MaNguoiDung))
                .ToDictionary(n => n.MaNguoiDung);

            var suatChieus = _context.SUATCHIEU
                .Include(s => s.PHIM)
                .Include(s => s.PHONGCHIEU)
                .Where(s => dsMaSuatChieu.Contains(s.MaSuatChieu))
                .ToDictionary(s => s.MaSuatChieu);

            foreach (var d in data)
            {
                if (nguoiDungs.TryGetValue(d.MaNguoiDung, out var nd))
                    d.NGUOIDUNG = nd;

                if (suatChieus.TryGetValue(d.MaSuatChieu, out var sc))
                    d.SUATCHIEU = sc;
            }

            // Số liệu tổng hợp theo từng trạng thái
            ViewBag.SoChuaThanhToan = _context.DONHANG.Count(x => x.TrangThai == "ChuaThanhToan");
            ViewBag.SoDaThanhToan = _context.DONHANG.Count(x => x.TrangThai == "DaThanhToan");
            ViewBag.SoHuy = _context.DONHANG.Count(x => x.TrangThai == "Huy");

            ViewBag.TrangThai = trangThai;
            ViewBag.TotalPage = Math.Ceiling((double)total / pageSize);
            ViewBag.CurrentPage = page;

            return View(data);
        }

        // ===============================
        // ✅ XÁC NHẬN ĐƠN ĐẶT VÉ
        // Dùng cho trường hợp thanh toán tại quầy hoặc xác nhận thủ công.
        // ===============================
        public IActionResult XacNhanDonHang(int id, string? trangThai)
        {
            var dh = _context.DONHANG.Find(id);

            if (dh == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("QuanLyDonHang", new { trangThai });
            }

            if (dh.TrangThai == "DaThanhToan")
            {
                TempData["Error"] = "Đơn hàng này đã được xác nhận trước đó!";
                return RedirectToAction("QuanLyDonHang", new { trangThai });
            }

            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    dh.TrangThai = "DaThanhToan";

                    // Kích hoạt toàn bộ vé thuộc đơn hàng
                    var veTrongDon = _context.VE
                        .Where(v => v.MaDonHang == dh.MaDonHang)
                        .ToList();

                    foreach (var ve in veTrongDon)
                    {
                        ve.TrangThai = 1;
                    }

                    // Ghi nhận bản ghi thanh toán nếu đơn chưa có
                    var veDaiDien = veTrongDon.OrderBy(v => v.MaVe).FirstOrDefault();

                    if (veDaiDien != null)
                    {
                        bool daCoThanhToan = _context.THANHTOAN
                            .Any(t => t.MaVe == veDaiDien.MaVe);

                        if (!daCoThanhToan)
                        {
                            _context.THANHTOAN.Add(new THANHTOAN
                            {
                                MaVe = veDaiDien.MaVe,
                                PhuongThuc = "TaiQuay",
                                SoTien = dh.TongTien,
                                TrangThai = "ThanhCong",
                                ThoiDiem = DateTime.Now
                            });
                        }
                    }

                    _context.SaveChanges();
                    tran.Commit();

                    TempData["Success"] = $"Đã xác nhận đơn hàng #{dh.MaDonHang}.";
                }
                catch
                {
                    tran.Rollback();
                    TempData["Error"] = "Xác nhận đơn hàng thất bại!";
                }
            }

            return RedirectToAction("QuanLyDonHang", new { trangThai });
        }

        // ===============================
        // 🔄 ĐỔI TRẠNG THÁI ĐƠN HÀNG
        // ===============================
        public IActionResult DoiTrangThaiDonHang(int id, string? trangThai)
        {
            var dh = _context.DONHANG.Find(id);

            if (dh == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("QuanLyDonHang", new { trangThai });
            }

            if (dh.TrangThai == "Huy")
                return XacNhanDonHang(id, trangThai);

            return HuyDonHang(id, trangThai);
        }
        public IActionResult ChiTietDonHang(int id)
        {
            // Nạp đơn hàng trước, không dùng Include để tránh bị loại bản ghi
            // khi suất chiếu liên kết không còn tồn tại trong dữ liệu.
            var donhang = _context.DONHANG.FirstOrDefault(x => x.MaDonHang == id);

            if (donhang == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("QuanLyDonHang");
            }

            // Nạp các dữ liệu liên quan một cách an toàn
            donhang.NGUOIDUNG = _context.NGUOIDUNG
                .FirstOrDefault(n => n.MaNguoiDung == donhang.MaNguoiDung);

            var suat = _context.SUATCHIEU
                .Include(s => s.PHIM)
                .Include(s => s.PHONGCHIEU)
                    .ThenInclude(p => p.RAP)
                .FirstOrDefault(s => s.MaSuatChieu == donhang.MaSuatChieu);

            if (suat != null)
                donhang.SUATCHIEU = suat;
            else
                ViewBag.CanhBao = "Suất chiếu của đơn hàng này không còn tồn tại trong dữ liệu.";

            ViewBag.Combo = _context.CT_DONHANG_COMBO
                .Include(x => x.COMBO)
                .Where(x => x.MaDonHang == id)
                .ToList();

            return View(donhang);
        }
        // ===============================
        // ❌ HỦY ĐƠN HÀNG VÀ HOÀN TRẢ GHẾ
        // Khi hủy đơn, toàn bộ vé thuộc đơn được xóa để giải phóng ghế,
        // tránh tình trạng ghế bị khóa vĩnh viễn.
        // ===============================
        public IActionResult HuyDonHang(int id, string? trangThai)
        {
            var dh = _context.DONHANG.Find(id);

            if (dh == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("QuanLyDonHang", new { trangThai });
            }

            if (dh.TrangThai == "Huy")
            {
                TempData["Error"] = "Đơn hàng này đã bị hủy trước đó!";
                return RedirectToAction("QuanLyDonHang", new { trangThai });
            }

            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var veTrongDon = _context.VE
                        .Where(v => v.MaDonHang == dh.MaDonHang)
                        .ToList();

                    var maVeList = veTrongDon.Select(v => v.MaVe).ToList();

                    // Xóa dữ liệu phụ thuộc trước khi xóa vé
                    var thanhToan = _context.THANHTOAN
                        .Where(t => maVeList.Contains(t.MaVe))
                        .ToList();

                    _context.THANHTOAN.RemoveRange(thanhToan);

                    var veCombo = _context.VE_COMBO
                        .Where(vc => maVeList.Contains(vc.MaVe))
                        .ToList();

                    _context.VE_COMBO.RemoveRange(veCombo);

                    // Xóa vé để trả ghế về trạng thái trống
                    _context.VE.RemoveRange(veTrongDon);

                    dh.TrangThai = "Huy";

                    _context.SaveChanges();
                    tran.Commit();

                    TempData["Success"] =
                        $"Đã hủy đơn hàng #{dh.MaDonHang} và hoàn trả {veTrongDon.Count} ghế.";
                }
                catch
                {
                    tran.Rollback();
                    TempData["Error"] = "Hủy đơn hàng thất bại!";
                }
            }

            return RedirectToAction("QuanLyDonHang", new { trangThai });
        }
        public IActionResult QuanLyVe(int page = 1)
        {
            int pageSize = 10;

            var query = _context.VE
                .Include(v => v.GHE)
                .Include(v => v.SUATCHIEU)
                    .ThenInclude(s => s.PHIM)
                .Include(v => v.SUATCHIEU)
                    .ThenInclude(s => s.PHONGCHIEU)
                .Include(v => v.DONHANG)
                .OrderByDescending(v => v.ThoiDiemDat);

            int total = query.Count();

            var list = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Combo = _context.VE_COMBO
                .Include(x => x.COMBO)
                .ToList();

            ViewBag.TotalPage = Math.Ceiling((double)total / pageSize);
            ViewBag.CurrentPage = page;

            return View(list);
        }
        public IActionResult ChiTietVe(int id)
        {
            var ve = _context.VE
                .Include(v => v.GHE)
                .Include(v => v.SUATCHIEU)
                    .ThenInclude(s => s.PHIM)
                .Include(v => v.SUATCHIEU)
                    .ThenInclude(s => s.PHONGCHIEU)
                .FirstOrDefault(v => v.MaVe == id);

            var combo = _context.VE_COMBO
                .Include(x => x.COMBO)
                .Where(x => x.MaVe == id)
                .ToList();

            ViewBag.Combo = combo;

            return View(ve);
        }
        public IActionResult ThemVe()
        {
            ViewBag.Phim = _context.PHIM.ToList();

            ViewBag.SuatChieu = _context.SUATCHIEU
                .Include(x => x.PHIM)
                .ToList();

            ViewBag.Ghe = _context.GHE.ToList();

            ViewBag.Combo = _context.COMBO.ToList();

            return View();
        }
        [HttpPost]
        public IActionResult ThemVe(VE v)
        {
            v.ThoiDiemDat = DateTime.Now;
            v.TrangThai = 0;

            _context.VE.Add(v);
            _context.SaveChanges();

            return RedirectToAction("QuanLyVe");
        }
        public IActionResult SuaVe(int id)
        {
            var data = _context.VE.Find(id);

            ViewBag.SuatChieu = _context.SUATCHIEU.ToList();
            ViewBag.Ghe = _context.GHE.ToList();

            return View(data);
        }
        [HttpPost]
        public IActionResult SuaVe(VE v)
        {
            _context.VE.Update(v);
            _context.SaveChanges();

            return RedirectToAction("QuanLyVe");
        }
        public IActionResult XoaVe(int id)
        {
            var ve = _context.VE.Find(id);

            if (ve != null)
            {
                _context.VE.Remove(ve);
                _context.SaveChanges();
            }

            return RedirectToAction("QuanLyVe");
        }
        public IActionResult QuanLyKhuyenMai(int page = 1)
        {
            int pageSize = 10;

            var query = _context.KHUYENMAI
                .OrderByDescending(x => x.NgayBatDau);

            int total = query.Count();

            var data = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPage = Math.Ceiling((double)total / pageSize);
            ViewBag.CurrentPage = page;

            return View(data);
        }
        public IActionResult ThemKhuyenMai()
        {
            return View();
        }
        [HttpPost]
        public IActionResult ThemKhuyenMai(KHUYENMAI k, IFormFile HinhFile)
        {
            if (HinhFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(HinhFile.FileName);

                string path = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/images",
                    fileName
                );

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    HinhFile.CopyTo(stream);
                }

                // 🔥 CHỈ LƯU TÊN FILE VÀO DB
                k.HinhAnh = fileName;
            }

            _context.KHUYENMAI.Add(k);
            _context.SaveChanges();

            return RedirectToAction("QuanLyKhuyenMai");
        }

        public IActionResult SuaKhuyenMai(int id)
        {
            var data = _context.KHUYENMAI.Find(id);

            return View(data);
        }
        [HttpPost]
        public IActionResult SuaKhuyenMai(KHUYENMAI k, IFormFile HinhFile)
        {
            var data = _context.KHUYENMAI.Find(k.MaKhuyenMai);

            if (HinhFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(HinhFile.FileName);

                string path = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/images",
                    fileName
                );

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    HinhFile.CopyTo(stream);
                }

                data.HinhAnh = fileName;
            }

            data.TieuDe = k.TieuDe;
            data.MoTa = k.MoTa;
            data.NgayBatDau = k.NgayBatDau;
            data.NgayKetThuc = k.NgayKetThuc;

            _context.SaveChanges();

            return RedirectToAction("QuanLyKhuyenMai");
        }
        public IActionResult XoaKhuyenMai(int id)
        {
            var data = _context.KHUYENMAI.Find(id);

            if (data != null)
            {
                _context.KHUYENMAI.Remove(data);
                _context.SaveChanges();
            }

            return RedirectToAction("QuanLyKhuyenMai");
        }
        public IActionResult QuanLyNguoiDung(int page = 1)
        {
            int pageSize = 10;

            var query = _context.NGUOIDUNG
     .OrderByDescending(x => x.VaiTro == "ADMIN")
     .ThenByDescending(x => x.NgayTao);

            int total = query.Count();

            var data = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPage = Math.Ceiling((double)total / pageSize);
            ViewBag.CurrentPage = page;

            return View(data);
        }
        public IActionResult ChiTietNguoiDung(int id)
        {
            var data = _context.NGUOIDUNG.Find(id);

            return View(data);
        }
        public IActionResult ThemNguoiDung()
        {
            return View();
        }
        [HttpPost]
        public IActionResult ThemNguoiDung(NGUOIDUNG u, IFormFile? AvatarFile)
        {
            ModelState.Remove(nameof(NGUOIDUNG.Avatar));

            string? loiMatKhau = MatKhauHelper.KiemTraDoManh(u.MatKhau);
            if (loiMatKhau != null)
                ModelState.AddModelError(nameof(NGUOIDUNG.MatKhau), loiMatKhau);

            if (!string.IsNullOrWhiteSpace(u.Email)
                && _context.NGUOIDUNG.Any(x => x.Email == u.Email))
            {
                ModelState.AddModelError(nameof(NGUOIDUNG.Email), "Email đã được sử dụng.");
            }

            string? loiAnh = UploadHelper.KiemTraAnh(AvatarFile);
            if (loiAnh != null)
                ModelState.AddModelError("AvatarFile", loiAnh);

            if (!ModelState.IsValid)
                return View(u);

            u.VaiTro = string.IsNullOrWhiteSpace(u.VaiTro)
                ? SessionKeys.VaiTroUser
                : u.VaiTro.ToUpperInvariant();

            u.NgayTao = DateTime.Now;
            u.MatKhau = MatKhauHelper.BamMatKhau(u.MatKhau);
            u.Avatar = UploadHelper.LuuAnh(AvatarFile, "images/avatars") ?? "default.png";

            _context.NGUOIDUNG.Add(u);
            _context.SaveChanges();

            TempData["Success"] = "Đã thêm người dùng mới.";
            return RedirectToAction("QuanLyNguoiDung");
        }
        public IActionResult SuaNguoiDung(int id)
        {
            var data = _context.NGUOIDUNG.Find(id);

            return View(data);
        }
        [HttpPost]
        public IActionResult SuaNguoiDung(NGUOIDUNG u, IFormFile? AvatarFile)
        {
            var data = _context.NGUOIDUNG.Find(u.MaNguoiDung);

            if (data == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng!";
                return RedirectToAction("QuanLyNguoiDung");
            }

            ModelState.Remove(nameof(NGUOIDUNG.Avatar));
            // Bỏ trống ô mật khẩu nghĩa là giữ nguyên mật khẩu cũ
            ModelState.Remove(nameof(NGUOIDUNG.MatKhau));

            if (!string.IsNullOrWhiteSpace(u.MatKhau))
            {
                string? loiMatKhau = MatKhauHelper.KiemTraDoManh(u.MatKhau);
                if (loiMatKhau != null)
                    ModelState.AddModelError(nameof(NGUOIDUNG.MatKhau), loiMatKhau);
            }

            if (!string.IsNullOrWhiteSpace(u.Email)
                && _context.NGUOIDUNG.Any(x => x.Email == u.Email && x.MaNguoiDung != u.MaNguoiDung))
            {
                ModelState.AddModelError(nameof(NGUOIDUNG.Email),
                    "Email này đã được tài khoản khác sử dụng.");
            }

            string? loiAnh = UploadHelper.KiemTraAnh(AvatarFile);
            if (loiAnh != null)
                ModelState.AddModelError("AvatarFile", loiAnh);

            if (!ModelState.IsValid)
                return View(u);

            string? tenAnh = UploadHelper.LuuAnh(AvatarFile, "images/avatars");
            if (tenAnh != null)
                data.Avatar = tenAnh;

            data.HoTen = u.HoTen;
            data.Email = u.Email;
            data.SoDienThoai = u.SoDienThoai;
            data.VaiTro = string.IsNullOrWhiteSpace(u.VaiTro)
                ? data.VaiTro
                : u.VaiTro.ToUpperInvariant();

            // Chỉ đổi mật khẩu khi quản trị viên thực sự nhập mật khẩu mới
            if (!string.IsNullOrWhiteSpace(u.MatKhau))
                data.MatKhau = MatKhauHelper.BamMatKhau(u.MatKhau);

            _context.SaveChanges();

            TempData["Success"] = "Đã cập nhật thông tin người dùng.";
            return RedirectToAction("QuanLyNguoiDung");
        }
        public IActionResult XoaNguoiDung(int id)
        {
            var data = _context.NGUOIDUNG.Find(id);

            if (data == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng!";
                return RedirectToAction("QuanLyNguoiDung");
            }

            // Không cho tự xóa tài khoản đang đăng nhập
            var maDangNhap = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            if (maDangNhap == id)
            {
                TempData["Error"] = "Không thể xóa tài khoản bạn đang đăng nhập!";
                return RedirectToAction("QuanLyNguoiDung");
            }

            // Không cho xóa người dùng đã phát sinh đơn hàng hoặc vé
            bool coDonHang = _context.DONHANG.Any(d => d.MaNguoiDung == id);
            bool coVe = _context.VE.Any(v => v.MaNguoiDung == id);

            if (coDonHang || coVe)
            {
                TempData["Error"] =
                    "Không thể xóa người dùng đã có đơn hàng hoặc vé. Bạn có thể khóa tài khoản thay vì xóa.";
                return RedirectToAction("QuanLyNguoiDung");
            }

            _context.NGUOIDUNG.Remove(data);
            _context.SaveChanges();

            TempData["Success"] = "Đã xóa người dùng.";
            return RedirectToAction("QuanLyNguoiDung");
        }
        public IActionResult ResetMatKhau(int id)
        {
            var user = _context.NGUOIDUNG.Find(id);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng!";
                return RedirectToAction("QuanLyNguoiDung");
            }

            const string matKhauMacDinh = "Miku@123";

            user.MatKhau = MatKhauHelper.BamMatKhau(matKhauMacDinh);

            _context.SaveChanges();

            TempData["Success"] =
                $"Đã đặt lại mật khẩu về \"{matKhauMacDinh}\". Vui lòng nhắc người dùng đổi lại sau khi đăng nhập.";

            return RedirectToAction("ChiTietNguoiDung", new { id = id });
        }
        public IActionResult QuanLyCombo()
        {
            var data = _context.COMBO.ToList();

            return View(data);
        }
        public IActionResult ThemCombo()
        {
            return View();
        }
        [HttpPost]
        public IActionResult ThemCombo(COMBO c, IFormFile HinhFile)
        {
            if (HinhFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(HinhFile.FileName);

                string path = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/images",
                    fileName
                );

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    HinhFile.CopyTo(stream);
                }

                c.HinhAnh = fileName;
            }

            _context.COMBO.Add(c);
            _context.SaveChanges();

            return RedirectToAction("QuanLyCombo");
        }
        public IActionResult SuaCombo(int id)
        {
            var data = _context.COMBO.Find(id);

            return View(data);
        }
        [HttpPost]
        public IActionResult SuaCombo(COMBO c, IFormFile HinhFile)
        {
            var data = _context.COMBO.Find(c.MaCombo);

            if (HinhFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(HinhFile.FileName);

                string path = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/images",
                    fileName
                );

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    HinhFile.CopyTo(stream);
                }

                data.HinhAnh = fileName;
            }

            data.TenCombo = c.TenCombo;
            data.MoTa = c.MoTa;
            data.Gia = c.Gia;

            _context.SaveChanges();

            return RedirectToAction("QuanLyCombo");
        }
        public IActionResult XoaCombo(int id)
        {
            var data = _context.COMBO.Find(id);

            _context.COMBO.Remove(data);

            _context.SaveChanges();

            return RedirectToAction("QuanLyCombo");
        }
        public IActionResult ThongTinAdmin()
        {
            var id = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            var admin = _context.NGUOIDUNG
                .FirstOrDefault(x => x.MaNguoiDung == id);

            return View(admin);
        }

        // ==========================================================
        // QUẢN LÝ CÂU HỎI THƯỜNG GẶP (nội dung hướng dẫn cho khách)
        // ==========================================================
        public IActionResult QuanLyCauHoi(string? search)
        {
            var query = _context.ChatBot.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string tk = search.Trim();
                query = query.Where(c => c.CauHoi.Contains(tk) || c.TraLoi.Contains(tk));
            }

            ViewBag.Search = search;

            return View(query.OrderBy(c => c.MaHoiDap).ToList());
        }

        public IActionResult ThemCauHoi()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ThemCauHoi(ChatBot c)
        {
            if (!ModelState.IsValid)
                return View(c);

            _context.ChatBot.Add(c);
            _context.SaveChanges();

            TempData["Success"] = "Đã thêm câu hỏi thường gặp.";
            return RedirectToAction("QuanLyCauHoi");
        }

        public IActionResult SuaCauHoi(int id)
        {
            var data = _context.ChatBot.Find(id);

            if (data == null)
            {
                TempData["Error"] = "Không tìm thấy câu hỏi!";
                return RedirectToAction("QuanLyCauHoi");
            }

            return View(data);
        }

        [HttpPost]
        public IActionResult SuaCauHoi(ChatBot c)
        {
            if (!ModelState.IsValid)
                return View(c);

            var data = _context.ChatBot.Find(c.MaHoiDap);

            if (data == null)
            {
                TempData["Error"] = "Không tìm thấy câu hỏi!";
                return RedirectToAction("QuanLyCauHoi");
            }

            data.CauHoi = c.CauHoi;
            data.TraLoi = c.TraLoi;

            _context.SaveChanges();

            TempData["Success"] = "Đã cập nhật câu hỏi thường gặp.";
            return RedirectToAction("QuanLyCauHoi");
        }

        public IActionResult XoaCauHoi(int id)
        {
            var data = _context.ChatBot.Find(id);

            if (data == null)
            {
                TempData["Error"] = "Không tìm thấy câu hỏi!";
                return RedirectToAction("QuanLyCauHoi");
            }

            _context.ChatBot.Remove(data);
            _context.SaveChanges();

            TempData["Success"] = "Đã xóa câu hỏi thường gặp.";
            return RedirectToAction("QuanLyCauHoi");
        }

        // ==========================================================
        // TIẾP NHẬN VÀ PHẢN HỒI YÊU CẦU HỖ TRỢ
        // ==========================================================
        public IActionResult QuanLyYeuCauHoTro(string? trangThai, int page = 1)
        {
            int pageSize = 10;

            var query = _context.YEUCAUHOTRO.AsQueryable();

            if (!string.IsNullOrEmpty(trangThai))
                query = query.Where(y => y.TrangThai == trangThai);

            query = query
                .OrderBy(y => y.TrangThai == YEUCAUHOTRO.TrangThaiDaTraLoi ? 1 : 0)
                .ThenByDescending(y => y.ThoiDiemGui);

            int total = query.Count();

            var data = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.SoMoi = _context.YEUCAUHOTRO.Count(y => y.TrangThai == YEUCAUHOTRO.TrangThaiMoi);
            ViewBag.SoDangXuLy = _context.YEUCAUHOTRO.Count(y => y.TrangThai == YEUCAUHOTRO.TrangThaiDangXuLy);
            ViewBag.SoDaTraLoi = _context.YEUCAUHOTRO.Count(y => y.TrangThai == YEUCAUHOTRO.TrangThaiDaTraLoi);

            ViewBag.TrangThai = trangThai;
            ViewBag.TotalPage = Math.Ceiling((double)total / pageSize);
            ViewBag.CurrentPage = page;

            return View(data);
        }

        public IActionResult ChiTietYeuCauHoTro(int id)
        {
            var yeuCau = _context.YEUCAUHOTRO
                .Include(y => y.NGUOIDUNG)
                .FirstOrDefault(y => y.MaYeuCau == id);

            if (yeuCau == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu hỗ trợ!";
                return RedirectToAction("QuanLyYeuCauHoTro");
            }

            // Vừa mở ra xem thì chuyển sang trạng thái đang xử lý
            if (yeuCau.TrangThai == YEUCAUHOTRO.TrangThaiMoi)
            {
                yeuCau.TrangThai = YEUCAUHOTRO.TrangThaiDangXuLy;
                _context.SaveChanges();
            }

            return View(yeuCau);
        }

        [HttpPost]
        public IActionResult TraLoiYeuCauHoTro(int MaYeuCau, string NoiDungTraLoi)
        {
            var yeuCau = _context.YEUCAUHOTRO.Find(MaYeuCau);

            if (yeuCau == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu hỗ trợ!";
                return RedirectToAction("QuanLyYeuCauHoTro");
            }

            if (string.IsNullOrWhiteSpace(NoiDungTraLoi))
            {
                TempData["Error"] = "Vui lòng nhập nội dung phản hồi!";
                return RedirectToAction("ChiTietYeuCauHoTro", new { id = MaYeuCau });
            }

            if (NoiDungTraLoi.Trim().Length > 2000)
            {
                TempData["Error"] = "Nội dung phản hồi không được vượt quá 2000 ký tự!";
                return RedirectToAction("ChiTietYeuCauHoTro", new { id = MaYeuCau });
            }

            yeuCau.NoiDungTraLoi = NoiDungTraLoi.Trim();
            yeuCau.TrangThai = YEUCAUHOTRO.TrangThaiDaTraLoi;
            yeuCau.ThoiDiemTraLoi = DateTime.Now;

            _context.SaveChanges();

            TempData["Success"] = $"Đã phản hồi yêu cầu #{MaYeuCau}.";
            return RedirectToAction("ChiTietYeuCauHoTro", new { id = MaYeuCau });
        }

        public IActionResult XoaYeuCauHoTro(int id)
        {
            var yeuCau = _context.YEUCAUHOTRO.Find(id);

            if (yeuCau == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu hỗ trợ!";
                return RedirectToAction("QuanLyYeuCauHoTro");
            }

            _context.YEUCAUHOTRO.Remove(yeuCau);
            _context.SaveChanges();

            TempData["Success"] = "Đã xóa yêu cầu hỗ trợ.";
            return RedirectToAction("QuanLyYeuCauHoTro");
        }
    }
}