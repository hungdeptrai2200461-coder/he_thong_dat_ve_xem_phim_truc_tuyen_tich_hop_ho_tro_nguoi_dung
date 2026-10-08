using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MIKUPhim.Data;
using MIKUPhim.Helpers;
using MIKUPhim.Models;

namespace MIKUPhim.Controllers
{
    [Route("ChatBot")]
    public class ChatBotController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IntentEngine _intentEngine;
        private readonly MovieMatcher _movieMatcher;
        private readonly GeminiService _gemini;

        private static readonly MovieContext _memory = new MovieContext();

        public ChatBotController(
            ApplicationDbContext context,
            IntentEngine intentEngine,
            MovieMatcher movieMatcher,
            GeminiService gemini)
        {
            _context = context;
            _intentEngine = intentEngine;
            _movieMatcher = movieMatcher;
            _gemini = gemini;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            string message = request?.message?.Trim().ToLower() ?? "";

            string intent = _intentEngine.DetectIntent(message);
            // 🔥 chặn từ gây nhiễu (rất quan trọng)
            if (message.Contains("phim mới"))
            {
                intent = "movie_list";
            }
            // 🔥 clear memory nếu quá 10 phút
            if (!_memory.IsValid())
            {
                _memory.Clear();
            }
            // ===============================
            // 🔥 THÊM NHẬN DIỆN PHIM HOT
            // ===============================
            if (message.Contains("phim hot") || message.Contains("bán chạy") || message.Contains("top phim"))
            {
                intent = "movie_hot";
            }

            if (message.Contains("menu"))
            {
                intent = "menu";
            }

            PHIM movie = null;

            // 🔥 Ưu tiên intent trước (tránh hiểu sai "mới" thành tên phim)
            if (intent != "movie_list" && intent != "movie_hot")
            {
                movie = _movieMatcher.FindMovieFromMessage(message);
            }

            if (movie == null)
            {
                var allMovies = await _context.PHIM.ToListAsync();

                movie = allMovies.FirstOrDefault(p =>
                {
                    var name = p.TenPhim.ToLower();
                    var words = name.Split(' ');

                    return message.Contains(name)
                        || name.Contains(message)
                        || message.Split(' ').Any(w => words.Any(n => n.Contains(w)));
                });
            }

            if (movie != null)
            {
                _memory.SetMovie(movie.MaPhim, movie.TenPhim);

                if (intent == "fallback" || intent == "movie_search")
                {
                    intent = "movie_search";
                }
            }

            // ===============================
            // 🤝 GREETING + MENU
            // ===============================
            if (intent == "greeting")
            {
                return Ok(new
                {
                    text = "Chào bạn 👋 Bạn muốn tìm phim gì hay cần mình gợi ý phim không?",
                    type = "quick_menu",
                    items = new[]
                    {
                        new { text = "🔥 Phim HOT", value = "phim hot" },
                        new { text = "🎬 Phim đang chiếu", value = "phim" },
                        new { text = "🎁 Khuyến mãi", value = "khuyến mãi" },
                        new { text = "🍿 Combo", value = "combo" }
                    }
                });
            }

            // ===============================
            // 📱 MENU
            // ===============================
            if (intent == "menu")
            {
                return Ok(new
                {
                    type = "quick_menu",
                    items = new[]
                    {
                        new { text = "🔥 Phim HOT", value = "phim hot" },
                        new { text = "🎬 Phim đang chiếu", value = "phim" },
                        new { text = "🎁 Khuyến mãi", value = "khuyến mãi" },
                        new { text = "🍿 Combo", value = "combo" }
                    }
                });
            }

            // ===============================
            // 🔥 PHIM HOT (THÊM MỚI - CHUẨN SQL)
            // ===============================
            if (intent == "movie_hot")
            {
                var hotMovies = await _context.VE
                    .Where(v => v.TrangThai == 1) // 🔥 CHỈ GHẾ ĐÃ THANH TOÁN
                    .Include(v => v.SUATCHIEU)
                    .ThenInclude(sc => sc.PHIM)
                    .GroupBy(v => v.SUATCHIEU.MaPhim)
                    .Select(g => new
                    {
                        MaPhim = g.Key,
                        SoLuongBan = g.Count(),
                        Phim = g.First().SUATCHIEU.PHIM
                    })
                    .OrderByDescending(x => x.SoLuongBan)
                    .Take(5)
                    .ToListAsync();

                return Ok(hotMovies.Select(x => new
                {
                    text = x.Phim.TenPhim + " 🔥",
                    image = x.Phim.Poster,
                    link = "/Home/Details/" + x.MaPhim
                }).ToList());
            }

            // ===============================
            // 🎬 DANH SÁCH PHIM
            // ===============================
            if (intent == "movie_list")
            {
                var movieList = await _context.PHIM
                    .Where(p => p.TrangThai == "dang_chieu")
                    .Select(p => new
                    {
                        text = p.TenPhim,
                        image = p.Poster,
                        link = "/Home/Details/" + p.MaPhim
                    })
                    .ToListAsync();

                return Ok(movieList);
            }

            if (intent == "movie_search" && movie != null)
            {
                string text =
                    $"Phim: {movie.TenPhim}\n" +
                    $"Thể loại: {movie.TheLoai}\n" +
                    $"Đạo diễn: {movie.DaoDien}\n" +
                    $"Diễn viên: {movie.DienVien}\n" +
                    $"Thời lượng: {movie.ThoiLuong} phút\n" +
                    $"Độ tuổi: {movie.DoTuoi}+\n\n" +
                    $"{movie.MoTa}";

                return Ok(new[]
                {
                    new
                    {
                        text = text,
                        image = movie.Poster,
                        link = "/Home/Details/" + movie.MaPhim
                    }
                });
            }

            // ===============================
            // 🎬 SUẤT CHIẾU
            // ===============================
            // ===============================
            // 🎬 SUẤT CHIẾU GẦN NHẤT (FIX CHUẨN)
            // ===============================
            if (intent == "showtime" && _memory.LastMovieId != null)
            {
                var now = DateTime.Now;

                var showtime = await _context.SUATCHIEU
                    .Include(s => s.PHONGCHIEU)
                    .ThenInclude(p => p.RAP)
                    .Where(s => s.MaPhim == _memory.LastMovieId &&
                        (
                            s.NgayChieu > now.Date ||
                            (s.NgayChieu == now.Date && s.GioBatDau >= now.TimeOfDay)
                        )
                    )
                    .OrderBy(s => s.NgayChieu)
                    .ThenBy(s => s.GioBatDau)
                    .FirstOrDefaultAsync();

                if (showtime == null)
                {
                    return Ok(new { text = "Hiện chưa có suất chiếu sắp tới cho phim này." });
                }

                // 🔥 LƯU SUẤT CHIẾU
                _memory.SetShowtime(showtime.MaSuatChieu, showtime.NgayChieu);

                string text =
                    $"Suất chiếu gần nhất của {_memory.LastMovieName}:\n" +
                    $"{showtime.NgayChieu:dd/MM} - {showtime.GioBatDau} tại {showtime.PHONGCHIEU.RAP.TenRap}";

                return Ok(new { text = text });
            }


            // ===============================
            // 🪑 KIỂM TRA GHẾ (FIX FULL)
            // ===============================
            if (intent == "seat_check")
            {
                // ❗ nếu chưa có suất chiếu
                if (_memory.LastShowtimeId == null)
                {
                    return Ok(new { text = "Bạn hãy chọn suất chiếu trước nhé 🎬" });
                }

                // 🔥 lấy đúng suất chiếu từ memory
                var showtime = await _context.SUATCHIEU
                    .Include(s => s.PHONGCHIEU)
                    .FirstOrDefaultAsync(s => s.MaSuatChieu == _memory.LastShowtimeId);

                if (showtime == null)
                {
                    return Ok(new { text = "Không tìm thấy suất chiếu." });
                }

                // 🔥 đếm vé đã thanh toán
                int sold = await _context.VE
                    .CountAsync(v => v.MaSuatChieu == _memory.LastShowtimeId && v.TrangThai == 1);

                // 🔥 tổng số ghế
                int total = await _context.GHE
                    .CountAsync(g => g.MaPhong == showtime.MaPhong);

                int remain = total - sold;

                string text =
                    $"Suất chiếu của {_memory.LastMovieName} còn {remain}/{total} ghế trống 🎟️";

                return Ok(new { text = text });
            }

            if (intent == "ticket_price")
            {
                string raw =
                    "Giá vé hiện tại: Ghế thường khoảng 70.000đ, ghế VIP khoảng 90.000đ và ghế đôi khoảng 150.000đ.";

                return Ok(new { text = raw });
            }

            if (intent == "combo")
            {
                var combos = await _context.COMBO
                    .Select(c => new
                    {
                        text = c.TenCombo + " - " + c.Gia + "đ",
                        image = c.HinhAnh
                    })
                    .ToListAsync();

                return Ok(combos);
            }

            if (message.Contains("khuyến mãi") || message.Contains("ưu đãi"))
            {
                var now = DateTime.Now;

                var danhSachKM = _context.KHUYENMAI
                    .Where(k => k.NgayBatDau <= now && k.NgayKetThuc >= now)
                    .OrderByDescending(k => k.NgayBatDau)
                    .Take(5)
                    .ToList();

                if (!danhSachKM.Any())
                {
                    return Ok(new
                    {
                        text = "Hiện tại chưa có khuyến mãi nào 😢"
                    });
                }

                var result = danhSachKM.Select(k => new
                {
                    text = $"🎉 {k.TieuDe}\n{k.MoTa}",
                    image = k.HinhAnh,
                    link = "/Home/KhuyenMai"
                });

                return Ok(result);
            }

            if (intent == "cinema_info")
            {
                var raps = await _context.RAP.ToListAsync();

                return Ok(raps.Select(r => new
                {
                    text = $"{r.TenRap} - {r.DiaChi}"
                }).ToList());
            }

            if (intent == "imax" && _memory.LastMovieId != null)
            {
                var imaxShowtimes =
                    await _context.SUATCHIEU
                    .Include(s => s.PHONGCHIEU)
                    .ThenInclude(p => p.RAP)
                    .Where(s => s.MaPhim == _memory.LastMovieId &&
                                s.PHONGCHIEU.TenPhong.Contains("IMAX"))
                    .ToListAsync();

                if (!imaxShowtimes.Any())
                {
                    return Ok(new { text = "Phim này hiện chưa có suất chiếu IMAX." });
                }

                string raw = $"Phim {_memory.LastMovieName} có suất chiếu IMAX:\n";

                foreach (var s in imaxShowtimes)
                {
                    raw += $"{s.GioBatDau} - {s.PHONGCHIEU.RAP.TenRap}\n";
                }

                return Ok(new { text = raw });
            }

            if (intent == "policy")
            {
                return Ok(new[]
                {
                    new {
                        text="Bạn có thể xem chính sách rạp tại đây:",
                        link="/Home/ChinhSach"
                    }
                });
            }

            if (intent == "recommend")
            {
                var recommendMovies = await _context.PHIM
                   .Where(p => p.TrangThai == "dang_chieu")
                    .OrderByDescending(p => p.MaPhim)
                    .Take(3)
                    .ToListAsync();

                return Ok(recommendMovies.Select(m => new
                {
                    text = m.TenPhim,
                    image = m.Poster,
                    link = "/Home/Details/" + m.MaPhim
                }).ToList());
            }

            if (intent == "genre")
            {
                string genre = "";

                if (message.Contains("hoạt hình")) genre = "Hoạt Hình";
                if (message.Contains("hành động")) genre = "Hành Động";

                var movies = await _context.PHIM
                    .Where(p => p.TheLoai.Contains(genre))
                    .Take(5)
                    .ToListAsync();

                return Ok(movies.Select(m => new {
                    text = m.TenPhim,
                    image = m.Poster,
                    link = "/Home/Details/" + m.MaPhim
                }));
            }

            if (intent == "fallback")
            {
                string aiResponse =
                    await _gemini.AskFallback(message);

                return Ok(new
                {
                    text = aiResponse
                });
            }

            return Ok(new
            {
                text = "Xin lỗi, Miku chưa hiểu câu hỏi này. Bạn có thể hỏi về phim, lịch chiếu, giá vé hoặc khuyến mãi nhé."
            });
        }

        public class ChatRequest
        {
            public string message { get; set; }
        }
    }
}