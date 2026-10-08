using Microsoft.EntityFrameworkCore;
using MIKUPhim.Models;
using System.Collections.Generic;

namespace MIKUPhim.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Khai báo bảng PHIM
        public DbSet<PHIM> PHIM { get; set; }
        public DbSet<RAP> RAP { get; set; }
        public DbSet<PHONGCHIEU> PHONGCHIEU { get; set; }
        public DbSet<SUATCHIEU> SUATCHIEU { get; set; }
        public DbSet<LICHCHIEU> LICHCHIEU { get; set; }
        public DbSet<GHE> GHE { get; set; }
        public DbSet<VE> VE { get; set; }
        public DbSet<NGUOIDUNG> NGUOIDUNG { get; set; }
        public DbSet<TINHTHANH> TINHTHANH { get; set; }
        public DbSet<COMBO> COMBO { get; set; }
        public DbSet<VE_COMBO> VE_COMBO { get; set; }
        public DbSet<THANHTOAN> THANHTOAN { get; set; }
        public DbSet<DONHANG> DONHANG { get; set; }
        public DbSet<KHUYENMAI> KHUYENMAI { get; set; }
        public DbSet<MIKUPhim.Models.ChatBot> ChatBot { get; set; }
        public DbSet<YEUCAUHOTRO> YEUCAUHOTRO { get; set; }

        public DbSet<CT_DONHANG_COMBO> CT_DONHANG_COMBO { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<VE_COMBO>()
                .HasKey(vc => new { vc.MaVe, vc.MaCombo });

            // Xóa người dùng thì giữ lại yêu cầu hỗ trợ (chỉ bỏ liên kết)
            modelBuilder.Entity<YEUCAUHOTRO>()
                .HasOne(y => y.NGUOIDUNG)
                .WithMany()
                .HasForeignKey(y => y.MaNguoiDung)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}