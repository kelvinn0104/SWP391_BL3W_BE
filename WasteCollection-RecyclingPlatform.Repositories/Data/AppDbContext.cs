using Microsoft.EntityFrameworkCore;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordReset> PasswordResets => Set<PasswordReset>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<VoucherCategory> VoucherCategories => Set<VoucherCategory>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<VoucherCode> VoucherCodes => Set<VoucherCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();

            entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(100);
            entity.Property(x => x.Role)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.Points).IsRequired();
            entity.Property(x => x.IsLocked).HasDefaultValue(false);
        });

        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.ToTable("password_resets");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId);
            entity.Property(x => x.CodeHash).HasMaxLength(255).IsRequired();
            entity.Property(x => x.ResetTokenHash).HasMaxLength(255);
        });

        modelBuilder.Entity<Area>(entity =>
        {
            entity.ToTable("areas");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.DistrictName).IsUnique(); // Prevent duplicate district names
            entity.Property(x => x.DistrictName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.MonthlyCapacityKg).HasPrecision(18, 2);
            entity.Property(x => x.ProcessedThisMonthKg).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Ward>(entity =>
        {
            entity.ToTable("wards");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.AreaId, x.Name }).IsUnique(); // Prevent duplicate ward names in same area
            entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
            entity.Property(x => x.CollectedKg).HasPrecision(18, 2);

            entity.HasOne(x => x.Area)
                .WithMany(x => x.Wards)
                .HasForeignKey(x => x.AreaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Collectors)
                .WithMany(x => x.Wards)
                .UsingEntity<Dictionary<string, object>>(
                    "ward_collectors",
                    j => j.HasOne<User>().WithMany().HasForeignKey("CollectorsId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Ward>().WithMany().HasForeignKey("WardsId").OnDelete(DeleteBehavior.Cascade));
        });

        modelBuilder.Entity<VoucherCategory>(entity =>
        {
            entity.ToTable("voucher_categories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.ToTable("vouchers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(255).IsRequired();
            entity.Property(x => x.ImageUrl).HasMaxLength(1000);
            
            entity.HasOne(x => x.Category)
                .WithMany(x => x.Vouchers)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VoucherCode>(entity =>
        {
            entity.ToTable("voucher_codes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();

            entity.HasOne(x => x.Voucher)
                .WithMany(x => x.Codes)
                .HasForeignKey(x => x.VoucherId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.UsedByUser)
                .WithMany()
                .HasForeignKey(x => x.UsedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
