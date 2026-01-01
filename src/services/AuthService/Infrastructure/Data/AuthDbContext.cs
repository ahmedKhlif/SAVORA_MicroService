using Microsoft.EntityFrameworkCore;
using Savora.AuthService.Domain.Entities;

namespace Savora.AuthService.Infrastructure.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(256);
            
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(512);
            
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}

