using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MIKUPhim.Data;
using MIKUPhim.Helpers;
using MIKUPhim.Models;
using System.Linq;
using System.Drawing;
using System.IO;
using QRCoder;

namespace MIKUPhim.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>Số ghế tối đa cho phép đặt trong một đơn hàng.</summary>
        private const int SoGheToiDaMoiDon = 8;

        public IActionResult Index(string? tuKhoa, string? theLoai, string? trangThai)
        {
            var truyVan = _context.PHIM.AsQueryable();

            // Tìm kiếm theo tên phim, thể loại, đạo diễn hoặc diễn viên
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim();

                truyVan = truyVan.Where(p =>
                    p.TenPhim.Contains(tk) ||
                    p.TheLoai.Contains(tk) ||
                    p.DaoDien.Contains(tk) ||
                    p.DienVien.Contains(tk));
            }

            if (!string.IsNullOrWhiteSpace(theLoai))
                truyVan = truyVan.Where(p => p.TheLoai.Contains(theLoai));

            if (!string.IsNullOrWhiteSpace(trangThai))
                truyVan = truyVan.Where(p => p.TrangThai == trangThai);

            var dsPhim = truyVan.OrderBy(p => p.TenPhim).ToList();

            // Danh sách thể loại để đổ vào ô lọc
            ViewBag.DanhSachTheLoai = _context.PHIM
                .Where(p => p.TheLoai != null && p.TheLoai != "")
                .Select(p => p.TheLoai)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.TheLoai = theLoai;
            ViewBag.TrangThai = trangThai;
            ViewBag.DangTimKiem = !string.IsNullOrWhiteSpace(tuKhoa)
                                  || !string.IsNullOrWhiteSpace(theLoai)
                                  || !string.IsNullOrWhiteSpace(trangThai);
            ViewBag.SoKetQua = dsPhim.Count;

            var khuyenMai = _context.KHUYENMAI
                .OrderByDescending(x => x.NgayBatDau)
                .Take(5)
                .ToList();

            ViewBag.KhuyenMai = khuyenMai;

            return View(dsPhim);
        }
        public IActionResult Details(int id)
        {
            var phim = _context.PHIM.FirstOrDefault(p => p.MaPhim == id);

            if (phim == null)
            {
                return NotFound();
            }

            return View(phim);
        }
        public IActionResult ChonSuatChieu(int id, DateTime? date)
        {
            var phim = _context.PHIM.Find(id);

            var tinhs = _context.TINHTHANH.ToList();

            DateTime today = DateTime.Today;
            DateTime selectedDate = date ?? today;

            TimeSpan now = DateTime.Now.TimeOfDay;

            // Lấy dữ liệu từ SQL trước
            var suats = _context.SUATCHIEU
                .Include(s => s.PHONGCHIEU)
                .ThenInclude(p => p.RAP)
                .Where(s => s.MaPhim == id && s.NgayChieu == selectedDate)
                .ToList();

            // Lọc suất chiếu quá 15 phút
            suats = suats
                .Where(s =>
                    s.NgayChieu > today ||
                    s.GioBatDau.Add(TimeSpan.FromMinutes(15)) > now
                )
                .OrderBy(s => s.GioBatDau)
                .ToList();

            ViewBag.Tinh = tinhs;
            ViewBag.Suat = suats;
            ViewBag.NgayDuocChon = selectedDate;

            return View(phim);
        }
        // GET: ChonGhe
        // Yêu cầu đăng nhập ngay từ bước này để khách không chọn xong ghế
        // rồi mới bị chuyển sang trang đăng nhập và mất các lựa chọn đã thực hiện.
        [DangNhap]
        public IActionResult ChonGhe(int id)
        {
            if (id <= 0)
                return RedirectToAction("Index");

            var maNguoiDung = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            var hetHan = DateTime.Now.AddMinutes(-10);

            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    // 🔥 BƯỚC 1: Lấy danh sách các vé đã hết hạn giữ ghế
                    // Điều kiện:
                    // - TrangThai == 0 → vé chưa thanh toán (đang giữ ghế)
                    // - ThoiDiemDat < hetHan → đã quá thời gian cho phép (ví dụ: 10 phút)
                    // => Các vé này cần bị hủy để giải phóng ghế
                    var veHetHan = _context.VE
                        .Where(v => v.TrangThai == 0 && v.ThoiDiemDat < hetHan)
                        .ToList();

                    // 🔥 BƯỚC 2: Lấy danh sách mã vé (MaVe) từ các vé hết hạn
                    // Mục đích: dùng để xóa các dữ liệu liên quan (ví dụ: combo đi kèm)
                    var maVeList = veHetHan.Select(v => v.MaVe).ToList();

                    // 🔥 BƯỚC 3: Tìm các bản ghi combo liên kết với các vé hết hạn
                    // Vì bảng VE_COMBO có khóa ngoại tới VE nên cần xóa trước
                    // để tránh lỗi ràng buộc dữ liệu (foreign key constraint)
                    var veCombo = _context.VE_COMBO
                        .Where(vc => maVeList.Contains(vc.MaVe))
                        .ToList();

                    // 🔥 BƯỚC 4: Xóa các bản ghi combo liên quan
                    // Đảm bảo không còn dữ liệu phụ thuộc trước khi xóa vé
                    _context.VE_COMBO.RemoveRange(veCombo);

                    // 🔥 BƯỚC 5: Xóa các vé đã hết hạn
                    // Sau khi xóa, các ghế tương ứng sẽ được giải phóng
                    // và có thể được người dùng khác đặt lại
                    _context.VE.RemoveRange(veHetHan);

                    // 🔥 BƯỚC 6: Lưu toàn bộ thay đổi xuống cơ sở dữ liệu
                    // Bao gồm cả xóa VE_COMBO và VE
                    _context.SaveChanges();

                    // ✅ BƯỚC 7: Xác nhận transaction thành công
                    // Toàn bộ thao tác xóa được áp dụng chính thức
                    tran.Commit();
                }
                catch
                {
                    // ❌ Nếu có lỗi xảy ra ở bất kỳ bước nào:
                    // - Rollback toàn bộ transaction
                    // - Đảm bảo dữ liệu không bị xóa dở dang hoặc sai lệch
                    tran.Rollback();
                }
            }

            var suat = _context.SUATCHIEU
                .Include(s => s.PHIM)
                .Include(s => s.PHONGCHIEU)
                    .ThenInclude(p => p.RAP)
                .FirstOrDefault(s => s.MaSuatChieu == id);

            if (suat == null)
                return NotFound();

            var dsGhe = _context.GHE
                .Where(g => g.MaPhong == suat.MaPhong)
                .OrderBy(g => g.DayGhe)
                .ThenBy(g => g.SoGhe)
                .ToList();

            // 🔥 KHÓA GHẾ (giữ + đã thanh toán)
            var thoiGianGiuCho = DateTime.Now.AddMinutes(-10);

            var gheKhongTheDat = _context.VE
                .Where(v => v.MaSuatChieu == id &&
                    (
                        v.TrangThai == 1 ||
                        (v.TrangThai == 0 && v.ThoiDiemDat > thoiGianGiuCho)
                    ))
                .Select(v => v.MaGhe)
                .Distinct()
                .ToList();

            ViewBag.GheDaBan = gheKhongTheDat;
            ViewBag.Suat = suat;
            ViewBag.MaSuatChieu = id; 

            return View(dsGhe);
        }
        // POST: DatGhe
        [HttpPost]
        [DangNhap]
        public IActionResult DatGhe(int maSuatChieu, List<int> danhSachGhe)
        {
            var maNguoiDung = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            if (maNguoiDung == null)
                return RedirectToAction("Login", "Account");

            if (danhSachGhe == null || !danhSachGhe.Any())
            {
                TempData["Loi"] = "Vui lòng chọn ghế!";
                return RedirectToAction("ChonGhe", new { id = maSuatChieu });
            }

            // 🔥 Bỏ ghế trùng do gửi lên nhiều lần
            danhSachGhe = danhSachGhe.Distinct().ToList();

            // 🔥 Giới hạn số ghế tối đa mỗi đơn
            if (danhSachGhe.Count > SoGheToiDaMoiDon)
            {
                TempData["Loi"] =
                    $"Mỗi đơn chỉ được đặt tối đa {SoGheToiDaMoiDon} ghế. Bạn đang chọn {danhSachGhe.Count} ghế.";
                return RedirectToAction("ChonGhe", new { id = maSuatChieu });
            }

            var suatKiemTra = _context.SUATCHIEU.Find(maSuatChieu);

            if (suatKiemTra == null)
            {
                TempData["Loi"] = "Suất chiếu không tồn tại!";
                return RedirectToAction("Index");
            }

            // 🔥 Không cho đặt suất đã chiếu xong
            if (suatKiemTra.NgayChieu < DateTime.Today ||
                (suatKiemTra.NgayChieu == DateTime.Today && suatKiemTra.GioBatDau < DateTime.Now.TimeOfDay))
            {
                TempData["Loi"] = "Suất chiếu này đã bắt đầu, không thể đặt vé!";
                return RedirectToAction("ChonSuatChieu", new { id = suatKiemTra.MaPhim });
            }

            // 🔥 Kiểm tra ghế yêu cầu có thuộc đúng phòng của suất chiếu không
            int soGheHopLe = _context.GHE
                .Count(g => danhSachGhe.Contains(g.MaGhe) && g.MaPhong == suatKiemTra.MaPhong);

            if (soGheHopLe != danhSachGhe.Count)
            {
                TempData["Loi"] = "Danh sách ghế không hợp lệ!";
                return RedirectToAction("ChonGhe", new { id = maSuatChieu });
            }

            // 🔥 Kiểm tra đặt vượt số lượng vé còn lại
            var thoiGianConHieuLuc = DateTime.Now.AddMinutes(-10);

            int tongGhe = _context.GHE.Count(g => g.MaPhong == suatKiemTra.MaPhong);

            int gheDaGiu = _context.VE.Count(v =>
                v.MaSuatChieu == maSuatChieu &&
                (v.TrangThai == 1 || (v.TrangThai == 0 && v.ThoiDiemDat > thoiGianConHieuLuc)));

            int gheConLai = tongGhe - gheDaGiu;

            if (danhSachGhe.Count > gheConLai)
            {
                TempData["Loi"] =
                    $"Suất chiếu chỉ còn {gheConLai} ghế trống, không đủ cho {danhSachGhe.Count} ghế bạn chọn.";
                return RedirectToAction("ChonGhe", new { id = maSuatChieu });
            }

            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    // 🔥 Check trùng ghế chuẩn rạp
                    var thoiGianGiuCho = DateTime.Now.AddMinutes(-10);

                    var gheBiTrung = _context.VE
                        .Where(v =>
                            v.MaSuatChieu == maSuatChieu &&
                            danhSachGhe.Contains(v.MaGhe) &&
                            (
                                v.TrangThai == 1 ||
                                (v.TrangThai == 0 && v.ThoiDiemDat > thoiGianGiuCho)
                            ))
                        .Any();

                    if (gheBiTrung)
                    {
                        tran.Rollback();
                        TempData["Loi"] = "Ghế đã có người giữ!";
                        return RedirectToAction("ChonGhe", new { id = maSuatChieu });
                    }

                    var suat = _context.SUATCHIEU.Find(maSuatChieu);

                    var gheList = _context.GHE
                        .Where(g => danhSachGhe.Contains(g.MaGhe))
                        .ToList();

                    decimal tongTien = 0;

                    var donHang = new DONHANG
                    {
                        MaNguoiDung = maNguoiDung.Value,
                        MaSuatChieu = maSuatChieu,
                        TongTien = 0,
                        TrangThai = "ChuaThanhToan",
                        ThoiDiem = DateTime.Now
                    };

                    _context.DONHANG.Add(donHang);
                    _context.SaveChanges();

                    // 🔥 tạo VE
                    foreach (var ghe in gheList)
                    {
                        decimal gia = suat.GiaVe + ghe.PhuThu;
                        tongTien += gia;

                        _context.VE.Add(new VE
                        {
                            MaNguoiDung = maNguoiDung.Value,
                            MaSuatChieu = maSuatChieu,
                            MaGhe = ghe.MaGhe,
                            MaDonHang = donHang.MaDonHang,
                            TrangThai = 0,
                            ThoiDiemDat = DateTime.Now
                        });
                    }

                    _context.SaveChanges(); // 🔥 BẮT BUỘC

                    donHang.TongTien = tongTien;
                    _context.SaveChanges();

                    tran.Commit();

                    // 🔥 SESSION
                    HttpContext.Session.SetString("StartTime", DateTime.Now.ToString());
                    HttpContext.Session.SetInt32("MaDonHang", donHang.MaDonHang);

                    return RedirectToAction("BapNuoc", new { maSuatChieu });
                }
                catch
                {
                    tran.Rollback();
                    TempData["Loi"] = "Lỗi đặt ghế!";
                    return RedirectToAction("ChonGhe", new { id = maSuatChieu });
                }
            }
        }
        [DangNhap]
        public IActionResult HuyGhe(int maSuatChieu)
        {
            try
            {
                var maNguoiDung = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

                // ❌ thiếu dữ liệu → bỏ qua luôn (tránh lỗi rác)
                if (maNguoiDung == null || maSuatChieu <= 0)
                {
                    Console.WriteLine("HuyGhe: thiếu dữ liệu");
                    return Ok();
                }

                // 🔥 Lấy toàn bộ vé CHƯA THANH TOÁN của user tại suất này
                var veList = _context.VE
                    .Where(v =>
                        v.MaNguoiDung == maNguoiDung &&
                        v.MaSuatChieu == maSuatChieu &&
                        v.TrangThai != 1   // 🔥 KHÔNG phải đã thanh toán
                    )
                    .ToList();

                if (!veList.Any())
                {
                    Console.WriteLine("HuyGhe: không có vé để xóa");
                    return Ok();
                }

                var maVeList = veList.Select(v => v.MaVe).ToList();

                // 🔥 XÓA COMBO TẠM (VE_COMBO)
                var comboList = _context.VE_COMBO
                    .Where(c => maVeList.Contains(c.MaVe))
                    .ToList();

                if (comboList.Any())
                {
                    _context.VE_COMBO.RemoveRange(comboList);
                    Console.WriteLine($"HuyGhe: đã xóa {comboList.Count} combo");
                }

                // 🔥 XÓA VÉ → TRẢ GHẾ
                _context.VE.RemoveRange(veList);

                // 🔥 SAVE
                _context.SaveChanges();

                Console.WriteLine($"HuyGhe: đã xóa {veList.Count} vé - Suất {maSuatChieu}");
            }
            catch (Exception ex)
            {
                // ❗ log lỗi để debug
                Console.WriteLine("Lỗi HuyGhe: " + ex.Message);
            }

            // 🔥 XÓA SESSION giữ ghế
            HttpContext.Session.Remove("StartTime");
            HttpContext.Session.Remove("TongTien");
            HttpContext.Session.Remove("TongTienCombo");

            return Ok();
        }

        [DangNhap]
        public IActionResult BapNuoc(int maSuatChieu)
        {
            var suat = _context.SUATCHIEU
                .Include(s => s.PHIM)
                .Include(s => s.PHONGCHIEU)
                    .ThenInclude(p => p.RAP)
                .FirstOrDefault(s => s.MaSuatChieu == maSuatChieu);

            if (suat == null)
                return RedirectToAction("Index");

            var maNguoiDung = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            if (maNguoiDung == null)
                return RedirectToAction("Login", "Account");

            var veChuaThanhToan = _context.VE
                .Where(v =>
                    v.MaSuatChieu == maSuatChieu &&
                    v.MaNguoiDung == maNguoiDung &&
                    v.TrangThai == 0)
                .ToList();

            var gheDangChon = veChuaThanhToan
                .Select(v => v.MaGhe)
                .ToList();

            var gheList = _context.GHE
                .Where(g => gheDangChon.Contains(g.MaGhe))
                .ToList();

            var tenGhe = gheList
                .Select(g => g.MaSoGhe)
                .ToList();

            // 🔥 SỬA PHẦN TÍNH TIỀN GHẾ (CỘNG PHỤ THU)
            decimal tongTienVe = 0;

            foreach (var ghe in gheList)
            {
                tongTienVe += suat.GiaVe + ghe.PhuThu;
            }

            decimal tongTienCombo = 0;

            decimal tongTien = tongTienVe + tongTienCombo;

            HttpContext.Session.SetString("TongTien", tongTien.ToString());

            ViewBag.Suat = suat;
            ViewBag.SoGhe = tenGhe.Count;
            ViewBag.TenGhe = string.Join(", ", tenGhe);
            ViewBag.TongTienVe = tongTienVe;
            ViewBag.TongTien = tongTien;
            ViewBag.MaSuatChieu = maSuatChieu;

            ViewBag.GheCon = _context.GHE.Count(g => g.MaPhong == suat.MaPhong)
                              - _context.VE.Count(v => v.MaSuatChieu == maSuatChieu && v.TrangThai == 1);

            ViewBag.TongGhe = _context.GHE.Count(g => g.MaPhong == suat.MaPhong);

            var combos = _context.COMBO.ToList();

            return View(combos);
        }

        [HttpPost]
        [DangNhap]
        public IActionResult LuuCombo(int maSuatChieu, List<int> soLuong)
        {
            var maNguoiDung = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            if (maNguoiDung == null)
                return RedirectToAction("Login", "Account");

            // 🎟️ Lấy vé chưa thanh toán
            var danhSachVe = _context.VE
                .Where(v =>
                    v.MaSuatChieu == maSuatChieu &&
                    v.MaNguoiDung == maNguoiDung &&
                    v.TrangThai == 0)
                .ToList();

            if (!danhSachVe.Any())
            {
                TempData["Loi"] = "Ghế đã hết hạn!";
                return RedirectToAction("ChonGhe", new { id = maSuatChieu });
            }

            var comboList = _context.COMBO.ToList();

            // 🧹 Xóa combo cũ
            var maVeList = danhSachVe.Select(v => v.MaVe).ToList();

            var oldCombo = _context.VE_COMBO
                .Where(c => maVeList.Contains(c.MaVe))
                .ToList();

            _context.VE_COMBO.RemoveRange(oldCombo);

            // 🔥 TÍNH TỔNG TIỀN COMBO
            decimal tongTienCombo = 0;

            // 🍿 Thêm combo mới
            foreach (var ve in danhSachVe)
            {
                for (int i = 0; i < comboList.Count; i++)
                {
                    int sl = (soLuong != null && i < soLuong.Count) ? soLuong[i] : 0;

                    if (sl > 0)
                    {
                        // 👉 lưu DB
                        _context.VE_COMBO.Add(new VE_COMBO
                        {
                            MaVe = ve.MaVe,
                            MaCombo = comboList[i].MaCombo,
                            SoLuong = sl
                        });

                        // 👉 tính tiền (chỉ tính 1 lần cho combo, tránh nhân theo số vé)
                        if (ve == danhSachVe.First())
                        {
                            tongTienCombo += comboList[i].Gia * sl;
                        }
                    }
                }
            }

            _context.SaveChanges();

            // 🔥 LƯU SESSION (CÁI QUAN TRỌNG NHẤT)
            HttpContext.Session.SetString("TongTienCombo", tongTienCombo.ToString());

            return RedirectToAction("ThanhToan", new { maSuatChieu });
        }
        [DangNhap]
        public IActionResult ThanhToan(int maSuatChieu)
        {
            var maNguoiDung = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            if (maNguoiDung == null)
                return RedirectToAction("Login", "Account");

            var suat = _context.SUATCHIEU
                .Include(s => s.PHIM)
                .Include(s => s.PHONGCHIEU)
                    .ThenInclude(p => p.RAP)
                .FirstOrDefault(s => s.MaSuatChieu == maSuatChieu);

            if (suat == null)
                return RedirectToAction("Index");

            var veChuaThanhToan = _context.VE
                .Where(v =>
                    v.MaSuatChieu == maSuatChieu &&
                    v.MaNguoiDung == maNguoiDung &&
                    v.TrangThai == 0)
                .ToList();
            if (!veChuaThanhToan.Any())
            {
                TempData["Loi"] = "Ghế đã hết hạn hoặc bị hủy!";
                return RedirectToAction("ChonGhe", new { id = maSuatChieu });
            }

            var gheDangChon = veChuaThanhToan
                .Select(v => v.MaGhe)
                .ToList();

            var gheList = _context.GHE
                .Where(g => gheDangChon.Contains(g.MaGhe))
                .ToList();

            var tenGhe = gheList
                .Select(g => g.MaSoGhe)
                .ToList();

            // 🔥 SỬA PHẦN TÍNH TIỀN GHẾ (CỘNG PHỤ THU)
            decimal tongTienVe = 0;

            foreach (var ghe in gheList)
            {
                tongTienVe += suat.GiaVe + ghe.PhuThu;
            }

            var comboSession = HttpContext.Session.GetString("TongTienCombo");

            decimal tongTienCombo = 0;

            if (!string.IsNullOrEmpty(comboSession))
            {
                tongTienCombo = decimal.Parse(comboSession);
            }

            decimal tongTien = tongTienVe + tongTienCombo;

            ViewBag.Suat = suat;
            ViewBag.TenGhe = string.Join(", ", tenGhe);
            ViewBag.TongTienVe = tongTienVe;
            ViewBag.MaSuatChieu = maSuatChieu;
            ViewBag.TongTien = tongTien;

            return View();
        }
        [DangNhap]
        public IActionResult ThanhToanVnPay(decimal tongTien, int maSuatChieu)
        {
            string vnp_Url =
                "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

            string vnp_TmnCode = "EYX3UVFI";

            string vnp_HashSecret =
                "2SR4JBJ5DLW7GMYE3PDFX89VSN8WRODK";

            string vnp_ReturnUrl =
                "https://localhost:7109/Home/VnPayReturn";

            VnPayLibrary vnp = new VnPayLibrary();

            vnp.AddRequestData("vnp_Version", "2.1.0");
            vnp.AddRequestData("vnp_Command", "pay");
            vnp.AddRequestData("vnp_TmnCode", vnp_TmnCode);

            vnp.AddRequestData("vnp_Amount",
                ((int)(tongTien * 100)).ToString());

            vnp.AddRequestData("vnp_CreateDate",
                DateTime.Now.ToString("yyyyMMddHHmmss"));

            vnp.AddRequestData("vnp_CurrCode", "VND");

            vnp.AddRequestData("vnp_IpAddr",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            vnp.AddRequestData("vnp_Locale", "vn");

            vnp.AddRequestData("vnp_OrderInfo",
                "ThanhToanVeXemPhim");

            vnp.AddRequestData("vnp_OrderType", "billpayment");

            // thêm dòng này
            vnp.AddRequestData("vnp_BankCode", "NCB");

            vnp.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);

            vnp.AddRequestData("vnp_TxnRef",
                DateTime.Now.ToString("yyyyMMddHHmmss"));

            string paymentUrl =
                vnp.CreateRequestUrl(vnp_Url, vnp_HashSecret);

            return Redirect(paymentUrl);
        }
        [DangNhap]
        public IActionResult VnPayReturn()
        {
            var responseCode = Request.Query["vnp_ResponseCode"];
            var maNguoiDung = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            if (responseCode == "00")
            {
                var donHang = _context.DONHANG
                .Where(d => d.MaNguoiDung == maNguoiDung && d.TrangThai == "ChuaThanhToan")
                .OrderByDescending(d => d.MaDonHang)
                .FirstOrDefault();

                decimal soTien =
                    decimal.Parse(Request.Query["vnp_Amount"]) / 100;

                if (donHang == null)
                {
                    return RedirectToAction("Index");
                }

                HttpContext.Session.Remove("StartTime");
                HttpContext.Session.Remove("TongTien");
                HttpContext.Session.Remove("TongTienCombo");

                donHang.TrangThai = "DaThanhToan";
                donHang.TongTien = soTien;
                var veChuaThanhToan = _context.VE
                    .Where(v => v.MaDonHang == donHang.MaDonHang && v.TrangThai == 0)
                    .ToList();

                if (veChuaThanhToan.Count == 0)
                    return View();

                foreach (var ve in veChuaThanhToan)
                {
                    ve.TrangThai = 1;
                }

                var veDaiDien = veChuaThanhToan.First();
                ViewBag.MaVe = veDaiDien.MaVe;
                // Lấy mã đơn hàng
                int maDonHang = donHang.MaDonHang;

                // Lấy combo đã chọn
                var combos = _context.VE_COMBO
                    .Where(c => c.MaVe == veDaiDien.MaVe)
                    .ToList();

                // Lưu vào CT_DONHANG_COMBO
                foreach (var c in combos)
                {
                    var ct = new CT_DONHANG_COMBO
                    {
                        MaDonHang = maDonHang,
                        MaCombo = c.MaCombo,
                        SoLuong = c.SoLuong
                    };

                    _context.CT_DONHANG_COMBO.Add(ct);
                }

                var thanhToan = new THANHTOAN
                {
                    MaVe = veDaiDien.MaVe,
                    PhuongThuc = "VNPAY",
                    SoTien = soTien,
                    TrangThai = "ThanhCong",
                    ThoiDiem = DateTime.Now
                };

                _context.THANHTOAN.Add(thanhToan);

                _context.SaveChanges();

                ViewBag.Message = "Thanh toán thành công";
                // Lấy thông tin suất chiếu
                var suat = _context.SUATCHIEU
                    .Include(s => s.PHIM)
                    .Include(s => s.PHONGCHIEU)
                    .ThenInclude(p => p.RAP)
                    .FirstOrDefault(s => s.MaSuatChieu == donHang.MaSuatChieu);

                // Lấy danh sách ghế
                var gheList = _context.VE
                    .Where(v => v.MaDonHang == donHang.MaDonHang)
                    .Select(v => v.MaGhe)
                    .ToList();

                var tenGhe = _context.GHE
                    .Where(g => gheList.Contains(g.MaGhe))
                    .Select(g => g.MaSoGhe)
                    .ToList();

                // truyền sang view
                ViewBag.Suat = suat;
                ViewBag.Ghe = string.Join(", ", tenGhe);
                ViewBag.MaDonHang = donHang.MaDonHang;
                ViewBag.TongTien = soTien;
                ViewBag.PhuongThuc = "VNPay";
            }
            else
            {
                ViewBag.Message = "Thanh toán thất bại";
            }

            return View();
        }
        [HttpPost]
        [DangNhap]
        public IActionResult ThanhToanOnline(
      string payment,
      decimal tongTien, // vẫn giữ nhưng KHÔNG dùng
      int maSuatChieu)
        {
            var maNguoiDung = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            var suat = _context.SUATCHIEU
                .FirstOrDefault(s => s.MaSuatChieu == maSuatChieu);

            var veChuaThanhToan = _context.VE
                .Where(v =>
                    v.MaSuatChieu == maSuatChieu &&
                    v.MaNguoiDung == maNguoiDung &&
                    v.TrangThai == 0)
                .ToList();

            var gheList = _context.GHE
                .Where(g => veChuaThanhToan.Select(v => v.MaGhe).Contains(g.MaGhe))
                .ToList();

            // 🔥 TÍNH LẠI TIỀN VÉ
            decimal tongTienVe = 0;
            foreach (var ghe in gheList)
            {
                tongTienVe += suat.GiaVe + ghe.PhuThu;
            }

            // 🔥 LẤY COMBO
            var comboSession = HttpContext.Session.GetString("TongTienCombo");

            decimal tongTienCombo = 0;
            if (!string.IsNullOrEmpty(comboSession))
            {
                tongTienCombo = decimal.Parse(comboSession);
            }

            // 🔥 TỔNG CHUẨN (QUAN TRỌNG NHẤT)
            tongTien = tongTienVe + tongTienCombo;

            // 🚀 REDIRECT
            if (payment == "VNPAY")
            {
                return RedirectToAction("ThanhToanVnPay",
                    new { tongTien, maSuatChieu });
            }

            // MoMo và ZaloPay dùng luồng thanh toán mô phỏng
            if (payment == "MOMO" || payment == "ZALOPAY")
            {
                return RedirectToAction("ThanhToanMoPhong",
                    new { phuongThuc = payment, tongTien, maSuatChieu });
            }

            return RedirectToAction("Index");
        }

        // ===============================
        // 💳 THANH TOÁN MÔ PHỎNG (MoMo / ZaloPay)
        // Hiển thị màn hình xác nhận trước khi ghi nhận thanh toán.
        // ===============================
        [DangNhap]
        public IActionResult ThanhToanMoPhong(string phuongThuc, decimal tongTien, int maSuatChieu)
        {
            var maNguoiDung = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            var suat = _context.SUATCHIEU
                .Include(s => s.PHIM)
                .Include(s => s.PHONGCHIEU)
                    .ThenInclude(p => p.RAP)
                .FirstOrDefault(s => s.MaSuatChieu == maSuatChieu);

            if (suat == null)
                return RedirectToAction("Index");

            var veChuaThanhToan = _context.VE
                .Where(v => v.MaSuatChieu == maSuatChieu &&
                            v.MaNguoiDung == maNguoiDung &&
                            v.TrangThai == 0)
                .ToList();

            if (!veChuaThanhToan.Any())
            {
                TempData["Loi"] = "Ghế đã hết hạn giữ chỗ hoặc bị hủy!";
                return RedirectToAction("ChonGhe", new { id = maSuatChieu });
            }

            var gheDangChon = veChuaThanhToan.Select(v => v.MaGhe).ToList();

            var tenGhe = _context.GHE
                .Where(g => gheDangChon.Contains(g.MaGhe))
                .Select(g => g.MaSoGhe)
                .ToList();

            ViewBag.PhuongThuc = phuongThuc == "MOMO" ? "MoMo" : "ZaloPay";
            ViewBag.MaPhuongThuc = phuongThuc;
            ViewBag.Suat = suat;
            ViewBag.TenGhe = string.Join(", ", tenGhe);
            ViewBag.TongTien = tongTien;
            ViewBag.MaSuatChieu = maSuatChieu;

            return View();
        }

        // ===============================
        // ✅ XÁC NHẬN THANH TOÁN MÔ PHỎNG
        // ===============================
        [HttpPost]
        [DangNhap]
        [ValidateAntiForgeryToken]
        public IActionResult XacNhanThanhToanMoPhong(string phuongThuc, int maSuatChieu)
        {
            var maNguoiDung = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            var donHang = _context.DONHANG
                .Where(d => d.MaNguoiDung == maNguoiDung && d.TrangThai == "ChuaThanhToan")
                .OrderByDescending(d => d.MaDonHang)
                .FirstOrDefault();

            if (donHang == null)
            {
                TempData["Loi"] = "Không tìm thấy đơn hàng cần thanh toán!";
                return RedirectToAction("Index");
            }

            var veChuaThanhToan = _context.VE
                .Where(v => v.MaDonHang == donHang.MaDonHang && v.TrangThai == 0)
                .ToList();

            if (!veChuaThanhToan.Any())
            {
                TempData["Loi"] = "Ghế đã hết hạn giữ chỗ!";
                return RedirectToAction("ChonGhe", new { id = maSuatChieu });
            }

            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    foreach (var ve in veChuaThanhToan)
                    {
                        ve.TrangThai = 1;
                    }

                    donHang.TrangThai = "DaThanhToan";

                    var veDaiDien = veChuaThanhToan.OrderBy(v => v.MaVe).First();

                    // Chuyển combo đã chọn sang chi tiết đơn hàng
                    var combos = _context.VE_COMBO
                        .Where(c => c.MaVe == veDaiDien.MaVe)
                        .ToList();

                    foreach (var c in combos)
                    {
                        _context.CT_DONHANG_COMBO.Add(new CT_DONHANG_COMBO
                        {
                            MaDonHang = donHang.MaDonHang,
                            MaCombo = c.MaCombo,
                            SoLuong = c.SoLuong
                        });
                    }

                    _context.THANHTOAN.Add(new THANHTOAN
                    {
                        MaVe = veDaiDien.MaVe,
                        PhuongThuc = phuongThuc == "MOMO" ? "MOMO" : "ZALOPAY",
                        SoTien = donHang.TongTien,
                        TrangThai = "ThanhCong",
                        ThoiDiem = DateTime.Now
                    });

                    _context.SaveChanges();
                    tran.Commit();
                }
                catch
                {
                    tran.Rollback();
                    TempData["Loi"] = "Thanh toán thất bại, vui lòng thử lại!";
                    return RedirectToAction("ThanhToan", new { maSuatChieu });
                }
            }

            HttpContext.Session.Remove("StartTime");
            HttpContext.Session.Remove("TongTien");
            HttpContext.Session.Remove("TongTienCombo");

            TempData["Success"] =
                $"Thanh toán qua {(phuongThuc == "MOMO" ? "MoMo" : "ZaloPay")} thành công!";

            return RedirectToAction("VeCuaToi");
        }
        [DangNhap]
        public IActionResult VeCuaToi()
        {
            // 🔐 Lấy user từ session
            var maNguoiDung = HttpContext.Session.GetInt32(SessionKeys.MaNguoiDung);

            if (maNguoiDung == null)
                return RedirectToAction("Login", "Account");

            // 🕒 Thời gian hiện tại
            DateTime now = DateTime.Now;

            // 🎟️ Lấy danh sách vé
            var ves = _context.VE
                .Include(v => v.GHE)

                // 🎬 Suất chiếu + phim
                .Include(v => v.SUATCHIEU)
                    .ThenInclude(s => s.PHIM)

                // 🏢 Rạp
                .Include(v => v.SUATCHIEU)
                    .ThenInclude(s => s.PHONGCHIEU)
                        .ThenInclude(p => p.RAP)

                // 🧾 Đơn hàng + combo (🔥 QUAN TRỌNG)
                .Include(v => v.DONHANG)
                    .ThenInclude(d => d.CT_DONHANG_COMBO)
                        .ThenInclude(c => c.COMBO)

                .Where(v =>
                    v.MaNguoiDung == maNguoiDung &&
                    v.TrangThai == 1 &&

                    // 🔥 CHỈ LẤY VÉ CHƯA HẾT HẠN
                    (
                        v.SUATCHIEU.NgayChieu > now.Date ||

                        (v.SUATCHIEU.NgayChieu == now.Date &&
                         v.SUATCHIEU.GioKetThuc >= now.TimeOfDay)
                    )
                )

                // 📅 Sắp xếp vé
                .OrderBy(v => v.SUATCHIEU.NgayChieu)
                .ThenBy(v => v.SUATCHIEU.GioBatDau)

                .ToList();

            return View(ves);
        }
        public IActionResult QrCode(int id)
        {
            var ve = _context.VE
                .Include(v => v.GHE)
                .Include(v => v.SUATCHIEU)
                    .ThenInclude(s => s.PHIM)
                .Include(v => v.DONHANG)
                    .ThenInclude(d => d.CT_DONHANG_COMBO)
                        .ThenInclude(c => c.COMBO)
                .FirstOrDefault(v => v.MaVe == id);

            if (ve == null)
                return NotFound();

            // 🎯 XÁC ĐỊNH VÉ ĐẦU TIÊN TRONG ĐƠN
            var firstVe = _context.VE
                .Where(v => v.MaDonHang == ve.MaDonHang)
                .OrderBy(v => v.MaVe)
                .FirstOrDefault();

            bool isFirstTicket = firstVe != null && firstVe.MaVe == ve.MaVe;

            // 🎟️ GIÁ VÉ
            var giaCoBan = ve.SUATCHIEU?.GiaVe ?? 0;
            var phuThu = ve.GHE?.PhuThu ?? 0;
            var giaVe = giaCoBan + phuThu;

            decimal tongCombo = 0;
            string comboText = "";

            // 🍿 CHỈ VÉ ĐẦU MỚI CÓ COMBO
            if (isFirstTicket &&
                ve.DONHANG?.CT_DONHANG_COMBO != null &&
                ve.DONHANG.CT_DONHANG_COMBO.Any())
            {
                tongCombo = ve.DONHANG.CT_DONHANG_COMBO
                    .Sum(c => c.SoLuong * c.COMBO.Gia);

                comboText = " | 🍿 " + string.Join(", ",
                    ve.DONHANG.CT_DONHANG_COMBO
                        .Select(c => $"{c.COMBO.TenCombo}x{c.SoLuong}"));
            }

            // 💰 TỔNG TIỀN
            var tongTien = giaVe + tongCombo;

            // 🔥 TEXT QR NGẮN GỌN - CHUYÊN NGHIỆP
            string text =
                $"🎬 {ve.SUATCHIEU?.PHIM?.TenPhim} | " +
                $"📅 {ve.SUATCHIEU?.NgayChieu:dd/MM/yyyy} | " +
                $"🕒 {ve.SUATCHIEU?.GioBatDau:hh\\:mm}-{ve.SUATCHIEU?.GioKetThuc:hh\\:mm} | " +
                $"🎟 {ve.GHE?.MaSoGhe}({ve.GHE?.LoaiGhe}) | " +
                $"💰 {tongTien:N0}đ" +
                comboText;

            // 🔲 TẠO QR
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);

            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(20);

            return File(qrCodeImage, "image/png");
        }
        public IActionResult CheckTicket(int id)
        {
            var ve = _context.VE
                .Include(v => v.GHE)
                .Include(v => v.SUATCHIEU)
                    .ThenInclude(s => s.PHIM)
                .Include(v => v.SUATCHIEU)
                    .ThenInclude(s => s.PHONGCHIEU)
                        .ThenInclude(p => p.RAP)
                .FirstOrDefault(v => v.MaVe == id);

            if (ve == null)
                return NotFound();

            return View(ve);
        }
        public IActionResult ChinhSach()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        
        public IActionResult KhuyenMai()
        {
            var list = _context.KHUYENMAI
                .OrderByDescending(x => x.NgayBatDau)
                .ToList();

            return View(list);
        }
        public IActionResult ChiTietKhuyenMai(int id)
        {
            var km = _context.KHUYENMAI
                .FirstOrDefault(x => x.MaKhuyenMai == id);

            if (km == null)
                return NotFound();

            return View(km);
        }
        public IActionResult QuangCaoKhuyenMai()
        {
            var list = _context.KHUYENMAI
                .OrderByDescending(x => x.NgayBatDau)
                .Take(5)
                .ToList();

            return View(list);
        }
    }
}