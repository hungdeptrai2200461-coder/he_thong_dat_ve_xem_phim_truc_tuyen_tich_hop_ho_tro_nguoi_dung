namespace MIKUPhim.Helpers
{
    public class MovieContext
    {
        // =========================
        // phim đang được hỏi
        // =========================

        public int? LastMovieId { get; set; }

        public string LastMovieName { get; set; }

        // =========================
        // suất chiếu đang hỏi
        // =========================

        public int? LastShowtimeId { get; set; }

        public DateTime? LastShowtimeTime { get; set; }

        // =========================
        // rạp đang hỏi
        // =========================

        public int? LastCinemaId { get; set; }

        // =========================
        // thời điểm cập nhật context
        // =========================

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // =========================
        // set movie
        // =========================

        public void SetMovie(int movieId, string movieName)
        {
            LastMovieId = movieId;
            LastMovieName = movieName;

            LastUpdated = DateTime.Now;
        }

        // =========================
        // set showtime
        // =========================

        public void SetShowtime(int showtimeId, DateTime showtimeTime)
        {
            LastShowtimeId = showtimeId;
            LastShowtimeTime = showtimeTime;

            LastUpdated = DateTime.Now;
        }

        // =========================
        // clear context
        // =========================

        public void Clear()
        {
            LastMovieId = null;
            LastMovieName = null;
            LastShowtimeId = null;
            LastShowtimeTime = null;
            LastCinemaId = null;
        }

        // =========================
        // kiểm tra context còn hợp lệ
        // =========================

        public bool IsValid()
        {
            return (DateTime.Now - LastUpdated).TotalMinutes < 10;
        }
    }
}