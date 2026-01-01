using Microsoft.EntityFrameworkCore;
using Savora.InterventionsService.Domain.Entities;

namespace Savora.InterventionsService.Infrastructure.Data;

public class InterventionsDbContext : DbContext
{
    public InterventionsDbContext(DbContextOptions<InterventionsDbContext> options) : base(options)
    {
    }

    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<Intervention> Interventions => Set<Intervention>();
    public DbSet<PartUsed> PartsUsed => Set<PartUsed>();
    public DbSet<Labor> Labors => Set<Labor>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Technician>(entity =>
        {
            entity.ToTable("Technicians");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email);

            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.Phone)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Skills)
                .HasColumnType("text[]");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<Intervention>(entity =>
        {
            entity.ToTable("Interventions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReclamationId);
            entity.HasIndex(e => e.TechnicianId);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.Notes)
                .HasMaxLength(2000);

            entity.Property(e => e.DiagnosticNotes)
                .HasMaxLength(2000);

            entity.Property(e => e.ResolutionNotes)
                .HasMaxLength(2000);

            entity.HasOne(e => e.Technician)
                .WithMany(t => t.Interventions)
                .HasForeignKey(e => e.TechnicianId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Labor)
                .WithOne(l => l.Intervention)
                .HasForeignKey<Labor>(l => l.InterventionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Invoice)
                .WithOne(i => i.Intervention)
                .HasForeignKey<Invoice>(i => i.InterventionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.TotalPartsAmount);
            entity.Ignore(e => e.TotalLaborAmount);
            entity.Ignore(e => e.TotalAmount);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<PartUsed>(entity =>
        {
            entity.ToTable("PartsUsed");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InterventionId);

            entity.Property(e => e.PartName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.PartReference)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.UnitPriceSnapshot)
                .HasPrecision(18, 2);

            entity.HasOne(e => e.Intervention)
                .WithMany(i => i.PartsUsed)
                .HasForeignKey(e => e.InterventionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.TotalPrice);
        });

        modelBuilder.Entity<Labor>(entity =>
        {
            entity.ToTable("Labors");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InterventionId).IsUnique();

            entity.Property(e => e.Hours)
                .HasPrecision(8, 2);

            entity.Property(e => e.HourlyRate)
                .HasPrecision(18, 2);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Ignore(e => e.TotalAmount);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoices");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InterventionId).IsUnique();
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();

            entity.Property(e => e.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.PartsTotal)
                .HasPrecision(18, 2);

            entity.Property(e => e.LaborTotal)
                .HasPrecision(18, 2);

            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.PdfPath)
                .HasMaxLength(500);
        });
    }
}

