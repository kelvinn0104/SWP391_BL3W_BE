using Microsoft.EntityFrameworkCore;
using WasteCollection_RecyclingPlatform.Repositories.Entities;

namespace WasteCollection_RecyclingPlatform.Repositories.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordReset> PasswordResets => Set<PasswordReset>();

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
        });

        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.ToTable("password_resets");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId);
            entity.Property(x => x.CodeHash).HasMaxLength(255).IsRequired();
            entity.Property(x => x.ResetTokenHash).HasMaxLength(255);
        });
    }
}
