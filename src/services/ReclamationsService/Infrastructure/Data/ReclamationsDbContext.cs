using Microsoft.EntityFrameworkCore;
using Savora.ReclamationsService.Domain.Entities;

namespace Savora.ReclamationsService.Infrastructure.Data;

public class ReclamationsDbContext : DbContext
{
    public ReclamationsDbContext(DbContextOptions<ReclamationsDbContext> options) : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Reclamation> Reclamations => Set<Reclamation>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<ReclamationHistory> ReclamationHistories => Set<ReclamationHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("Clients");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
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

            entity.Property(e => e.Address)
                .HasMaxLength(500);

            entity.Property(e => e.City)
                .HasMaxLength(100);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<Reclamation>(entity =>
        {
            entity.ToTable("Reclamations");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ClientId);
            entity.HasIndex(e => e.ClientArticleId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(4000);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.Priority)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.ClosedBy)
                .HasMaxLength(256);

            entity.HasOne(e => e.Client)
                .WithMany(c => c.Reclamations)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.ToTable("Attachments");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReclamationId);

            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.ContentType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Path)
                .IsRequired()
                .HasMaxLength(1000);

            entity.HasOne(e => e.Reclamation)
                .WithMany(r => r.Attachments)
                .HasForeignKey(e => e.ReclamationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReclamationHistory>(entity =>
        {
            entity.ToTable("ReclamationHistories");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReclamationId);

            entity.Property(e => e.OldStatus)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.NewStatus)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.OldPriority)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.NewPriority)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.ActionType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Comment)
                .HasMaxLength(1000);

            entity.Property(e => e.ChangedBy)
                .IsRequired()
                .HasMaxLength(256);

            entity.HasOne(e => e.Reclamation)
                .WithMany(r => r.History)
                .HasForeignKey(e => e.ReclamationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsRead);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(e => e.NotificationType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.RelatedEntityType)
                .HasMaxLength(50);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("Messages");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SenderId);
            entity.HasIndex(e => e.ReceiverId);
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => new { e.SenderId, e.ReceiverId });

            entity.Property(e => e.Subject)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Content)
                .IsRequired()
                .HasMaxLength(4000);
        });
    }
}

