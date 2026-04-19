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
    public DbSet<CollectionRequest> CollectionRequests => Set<CollectionRequest>();
    public DbSet<WasteCategory> WasteCategories => Set<WasteCategory>();
    public DbSet<WasteReport> WasteReports => Set<WasteReport>();
    public DbSet<WasteReportItem> WasteReportItems => Set<WasteReportItem>();
    public DbSet<WasteReportImage> WasteReportImages => Set<WasteReportImage>();
    public DbSet<WasteReportStatusHistory> WasteReportStatusHistories => Set<WasteReportStatusHistory>();
    public DbSet<RewardPointTransaction> RewardPointTransactions => Set<RewardPointTransaction>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<ComplaintEvidence> ComplaintEvidenceFiles => Set<ComplaintEvidence>();

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

        modelBuilder.Entity<CollectionRequest>(entity =>
        {
            entity.ToTable("collection_requests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.WeightKg).HasPrecision(18, 2);

            entity.HasOne(x => x.Citizen)
                .WithMany()
                .HasForeignKey(x => x.CitizenId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Collector)
                .WithMany()
                .HasForeignKey(x => x.CollectorId)
                .OnDelete(DeleteBehavior.SetNull);
 
            entity.HasOne(x => x.Ward)
                .WithMany()
                .HasForeignKey(x => x.WardId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WasteCategory>(entity =>
        {
            entity.ToTable("waste_categories");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Unit).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.PointsPerKg).HasDefaultValue(100);
        });

        modelBuilder.Entity<WasteReport>(entity =>
        {
            entity.ToTable("waste_reports");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CitizenId);
            entity.HasIndex(x => x.AssignedCollectorId);
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.Title).HasMaxLength(255);
            entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.LocationText).HasMaxLength(1000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.CompletionNote).HasMaxLength(1000);
            entity.Property(x => x.ActualTotalWeightKg).HasPrecision(18, 2);

            entity.HasOne(x => x.Citizen)
                .WithMany()
                .HasForeignKey(x => x.CitizenId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AssignedCollector)
                .WithMany()
                .HasForeignKey(x => x.AssignedCollectorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.ToTable("complaints");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.WasteReportId);
            entity.HasIndex(x => x.CitizenId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => new { x.WasteReportId, x.CitizenId }).IsUnique();
            entity.Property(x => x.Reason).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.AdminNote).HasMaxLength(1000);

            entity.HasOne(x => x.WasteReport)
                .WithMany(x => x.Complaints)
                .HasForeignKey(x => x.WasteReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Citizen)
                .WithMany()
                .HasForeignKey(x => x.CitizenId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ResolvedByUser)
                .WithMany()
                .HasForeignKey(x => x.ResolvedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ComplaintEvidence>(entity =>
        {
            entity.ToTable("complaint_evidence_files");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ComplaintId);
            entity.Property(x => x.FileUrl).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.OriginalFileName).HasMaxLength(255);
            entity.Property(x => x.ContentType).HasMaxLength(100);

            entity.HasOne(x => x.Complaint)
                .WithMany(x => x.EvidenceFiles)
                .HasForeignKey(x => x.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RewardPointTransaction>(entity =>
        {
            entity.ToTable("reward_point_transactions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.Property(x => x.TransactionType).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WasteReportItem>(entity =>
        {
            entity.ToTable("waste_report_items");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.WasteReportId);
            entity.HasIndex(x => x.WasteCategoryId);
            entity.Property(x => x.EstimatedWeightKg).HasPrecision(18, 2);
            entity.Property(x => x.ActualWeightKg).HasPrecision(18, 2);

            entity.HasOne(x => x.WasteReport)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.WasteReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.WasteCategory)
                .WithMany(x => x.ReportItems)
                .HasForeignKey(x => x.WasteCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WasteReportImage>(entity =>
        {
            entity.ToTable("waste_report_images");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.WasteReportId);
            entity.HasIndex(x => x.WasteReportItemId);
            entity.Property(x => x.ImageUrl).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.OriginalFileName).HasMaxLength(255);
            entity.Property(x => x.ContentType).HasMaxLength(100);
            entity.Property(x => x.Purpose)
                .HasMaxLength(32)
                .HasDefaultValue(WasteReportImagePurpose.ReportEvidence)
                .IsRequired();

            entity.HasOne(x => x.WasteReport)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.WasteReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.WasteReportItem)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.WasteReportItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WasteReportStatusHistory>(entity =>
        {
            entity.ToTable("waste_report_status_histories");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.WasteReportId);
            entity.HasIndex(x => x.ChangedByUserId);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.Note).HasMaxLength(500);

            entity.HasOne(x => x.WasteReport)
                .WithMany(x => x.StatusHistories)
                .HasForeignKey(x => x.WasteReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ChangedByUser)
                .WithMany()
                .HasForeignKey(x => x.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
