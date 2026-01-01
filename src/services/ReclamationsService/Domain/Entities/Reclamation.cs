using Savora.Shared.Enums;

namespace Savora.ReclamationsService.Domain.Entities;

public class Reclamation
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Guid ClientArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Priority Priority { get; set; } = Priority.Medium;
    public ReclamationStatus Status { get; set; } = ReclamationStatus.New;
    public DateTime? SlaDeadline { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Navigation
    public Client Client { get; set; } = null!;
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<ReclamationHistory> History { get; set; } = new List<ReclamationHistory>();

    // Computed SLA status
    public string GetSlaStatus()
    {
        if (!SlaDeadline.HasValue || Status == ReclamationStatus.Closed || Status == ReclamationStatus.Cancelled)
            return "OnTime";

        var now = DateTime.UtcNow;
        var hoursUntilDeadline = (SlaDeadline.Value - now).TotalHours;

        if (now > SlaDeadline.Value)
            return "Overdue";
        if (hoursUntilDeadline <= 24)
            return "NearDeadline";
        return "OnTime";
    }

    // Calculate SLA deadline based on priority
    public void SetSlaDeadline()
    {
        var hoursToResolve = Priority switch
        {
            Priority.Urgent => 4,
            Priority.High => 24,
            Priority.Medium => 72,
            Priority.Low => 168, // 7 days
            _ => 72
        };

        SlaDeadline = CreatedAt.AddHours(hoursToResolve);
    }
}

