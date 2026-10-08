using MIKUPhim.Data;
using MIKUPhim.Models;
using Microsoft.EntityFrameworkCore;

namespace MIKUPhim.Helpers
{
    public class MovieMatcher
    {
        private readonly ApplicationDbContext _context;

        public MovieMatcher(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // Tìm phim trong câu hỏi người dùng
        // ==========================================

        public PHIM FindMovieFromMessage(string message)
        {
            message = Normalize(message);

            var movies = _context.PHIM.ToList();

            PHIM bestMovie = null;

            double bestScore = 0;

            foreach (var movie in movies)
            {
                double score = CalculateSimilarity(message, movie.TenPhim);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMovie = movie;
                }
            }

            if (bestScore < 0.6)
                return null;

            return bestMovie;
        }

        // ==========================================
        // Normalize text
        // ==========================================

        private string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // 1. về chữ thường
            text = text.ToLower();

            // 2. bỏ dấu tiếng Việt
            text = RemoveVietnameseSigns(text);

            // 3. bỏ ký tự đặc biệt
            text = text.Replace("?", " ")
                       .Replace(".", " ")
                       .Replace(",", " ")
                       .Replace("!", " ")
                       .Replace(":", " ")
                       .Replace(";", " ")
                       .Replace("-", " ")
                       .Replace("_", " ");

            // 4. loại bỏ từ "rác" (stopwords)
            string[] stopWords = new string[]
            {
        "phim", "xem", "co", "khong", "có", "không",
        "cho", "toi", "tôi", "muon", "muốn",
        "giup", "giúp", "ve", "vé", "rap", "rạp",
        "nay", "nay", "di", "đi", "voi", "với"
            };

            foreach (var word in stopWords)
            {
                text = text.Replace(" " + word + " ", " ");
            }

            // 5. loại bỏ khoảng trắng thừa
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }

        // ==========================================
        // Similarity
        // ==========================================

        private double CalculateSimilarity(string input, string movieName)
        {
            input = input.ToLower();
            movieName = movieName.ToLower();

            // chuẩn hóa bỏ dấu (quan trọng)
            input = RemoveVietnameseSigns(input);
            movieName = RemoveVietnameseSigns(movieName);

            // nếu chứa trực tiếp
            if (input.Contains(movieName))
                return 1.0;

            int distance = LevenshteinDistance(input, movieName);

            int maxLen = Math.Max(input.Length, movieName.Length);

            // tính độ giống (0 → 1)
            double similarity = 1.0 - (double)distance / maxLen;

            return similarity;
        }

        // ==========================================
        // xử lý sai chính tả nhẹ
        // ==========================================
        private string RemoveVietnameseSigns(string text)
        {
            string[] signs = new string[]
            {
        "aàáạảãâầấậẩẫăằắặẳẵ",
        "eèéẹẻẽêềếệểễ",
        "iìíịỉĩ",
        "oòóọỏõôồốộổỗơờớợởỡ",
        "uùúụủũưừứựửữ",
        "yỳýỵỷỹ",
        "dđ"
            };

            for (int i = 0; i < signs.Length; i++)
            {
                for (int j = 1; j < signs[i].Length; j++)
                {
                    text = text.Replace(signs[i][j], signs[i][0]);
                }
            }

            return text;
        }
        private bool IsSimilar(string a, string b)
        {
            if (a == b)
                return true;

            int distance = LevenshteinDistance(a, b);

            // 🔥 chỉ cho phép sai 1 ký tự với từ dài
            return distance == 1 && a.Length > 4;
        }

        // ==========================================
        // Levenshtein distance
        // ==========================================

        private int LevenshteinDistance(string s, string t)
        {
            int[,] d = new int[s.Length + 1, t.Length + 1];

            for (int i = 0; i <= s.Length; i++)
                d[i, 0] = i;

            for (int j = 0; j <= t.Length; j++)
                d[0, j] = j;

            for (int i = 1; i <= s.Length; i++)
            {
                for (int j = 1; j <= t.Length; j++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(
                            d[i - 1, j] + 1,
                            d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[s.Length, t.Length];
        }
    }
}