using lucky_draw.Models;
using Microsoft.EntityFrameworkCore;

namespace lucky_draw.Data
{
    // Code First: cấu trúc database được quản lý bằng migration trong thư mục Migrations.
    // Sửa model ở đây => chạy "dotnet ef migrations add <TenMigration>" để tạo migration mới.
    public class LuckyDrawDbContext : DbContext
    {
        public LuckyDrawDbContext(DbContextOptions<LuckyDrawDbContext> options) : base(options) { }

        public DbSet<Reward> Rewards { get; set; }
        public DbSet<RewardType> RewardTypes { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerInfo> CustomerInfos { get; set; }
        public DbSet<CustomerReward> CustomerReward { get; set; }
        public DbSet<Temptable> Temptable { get; set; }
        public DbSet<LuckyProgram> Programs { get; set; }
        public DbSet<CustomerRewardHistory> CustomerRewardHistory { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cột mã khách hàng là varchar: khai báo đúng để EF gửi tham số varchar,
            // tránh SQL Server phải convert nvarchar làm mất tác dụng index khi tìm theo mã khách hàng
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK_Customers_1");
                entity.Property(e => e.Id).ValueGeneratedNever(); // Id lấy từ file import, không tự tăng
                entity.Property(e => e.CustomerCode).HasMaxLength(30).IsUnicode(false);
                entity.Property(e => e.Name).HasMaxLength(255);

                // Tìm khách theo mã (trang Khách hàng tham gia) và procedure import kiểm tra mã đã có
                entity.HasIndex(e => e.CustomerCode, "IX_Customers_CustomerCode");
            });

            modelBuilder.Entity<CustomerInfo>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.CustomerCode).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.ClientNo).HasMaxLength(20).IsUnicode(false);
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.Property(e => e.BranchCode).HasMaxLength(4).IsUnicode(false).IsFixedLength();
                entity.Property(e => e.BranchName).HasMaxLength(255);
                entity.Property(e => e.AccountNo).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.IdNo).HasMaxLength(255).IsUnicode(false);
                entity.Property(e => e.AddNo).HasMaxLength(255);
                entity.Property(e => e.TelNo).HasMaxLength(255);
                entity.Property(e => e.Kind).HasMaxLength(255);
                entity.Property(e => e.Amount).HasColumnType("money");
                entity.Property(e => e.Time).HasColumnType("datetime");

                // Tra thông tin người trúng theo mã khách hàng (kèm sẵn IdNo, TelNo để không phải đọc lại bảng)
                entity.HasIndex(e => e.CustomerCode, "IX_CustomerInfo_CustomerCode")
                    .IncludeProperties(e => new { e.IdNo, e.TelNo });
            });

            modelBuilder.Entity<CustomerReward>(entity =>
            {
                entity.Property(e => e.ResultCode).HasMaxLength(50);
                entity.Property(e => e.TimeStamp).HasColumnType("datetime").HasDefaultValueSql("(getdate())");

                // Quay số kiểm tra "đã trúng chưa" theo CustomerId và đếm số người trúng theo RewardId
                entity.HasIndex(e => e.CustomerId, "IX_CustomerReward_CustomerId");
                entity.HasIndex(e => e.RewardId, "IX_CustomerReward_RewardId");
            });

            modelBuilder.Entity<CustomerRewardHistory>(entity =>
            {
                entity.HasNoKey();
                entity.Property(e => e.ResultCode).HasMaxLength(50);
                entity.Property(e => e.TimeStamp).HasColumnType("datetime");
            });

            modelBuilder.Entity<LuckyProgram>(entity =>
            {
                entity.Property(e => e.DateCreated).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
                entity.Property(e => e.DateStart).HasColumnType("datetime");
                entity.Property(e => e.ProgramName).HasMaxLength(255);
                entity.Property(e => e.ProgramFilePath).HasMaxLength(255).IsUnicode(false);
            });

            modelBuilder.Entity<Reward>(entity =>
            {
                entity.Property(e => e.RewardName).HasMaxLength(50);
                entity.Property(e => e.PrefixCode).HasMaxLength(20).IsUnicode(false);
                // Giá trị mặc định viết đúng như database gốc
                entity.Property(e => e.NumberOfReward).HasDefaultValueSql("((0))");
                entity.Property(e => e.RewardType).HasDefaultValueSql("((0))");
                entity.Property(e => e.AllowPreviousResultCode).HasDefaultValueSql("((0))");
                entity.Property(e => e.AllowMultiResultCode).HasDefaultValueSql("((0))");
            });

            modelBuilder.Entity<RewardType>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedOnAdd(); // tinyint IDENTITY
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.Description).HasColumnType("ntext");
                entity.Property(e => e.RewardOption).HasMaxLength(255);
            });

            modelBuilder.Entity<Temptable>(entity =>
            {
                // Bảng tạm nhận dữ liệu import (SqlBulkCopy), không có khóa chính
                entity.HasNoKey().ToTable("temptable");
                foreach (var column in new[] { "BranchCode", "CustomerId", "CustomerCode", "AccountNo", "IdNumber", "ClientNo", "CardNo" })
                {
                    entity.Property<string?>(column).HasMaxLength(250).IsUnicode(false);
                }
                // Tên, tên chi nhánh, địa chỉ có tiếng Việt => nvarchar (varchar làm mất dấu thành '?')
                foreach (var column in new[] { "FullName", "BranchName", "Address" })
                {
                    entity.Property<string?>(column).HasMaxLength(250);
                }
            });
        }
    }
}
