using Microsoft.EntityFrameworkCore;
using Savora.ArticlesService.Domain.Entities;

namespace Savora.ArticlesService.Infrastructure.Data;

public class ArticlesDbContext : DbContext
{
    public ArticlesDbContext(DbContextOptions<ArticlesDbContext> options) : base(options)
    {
    }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Article>(entity =>
        {
            entity.ToTable("Articles");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Reference).IsUnique();
            entity.HasIndex(e => e.SerialNumber);
            entity.HasIndex(e => e.ClientId);

            entity.Property(e => e.Reference)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Brand)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.SerialNumber)
                .IsRequired()
                .HasMaxLength(100);

            entity.Ignore(e => e.IsUnderWarranty);
            entity.Ignore(e => e.WarrantyEndDate);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<Part>(entity =>
        {
            entity.ToTable("Parts");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Reference).IsUnique();

            entity.Property(e => e.Reference)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(e => e.Category)
                .HasMaxLength(100);

            entity.Ignore(e => e.IsLowStock);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("StockMovements");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PartId);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.MovementType)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Reason)
                .HasMaxLength(500);

            entity.Property(e => e.RelatedEntityType)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(256);

            entity.HasOne(e => e.Part)
                .WithMany(p => p.StockMovements)
                .HasForeignKey(e => e.PartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

    }
}

