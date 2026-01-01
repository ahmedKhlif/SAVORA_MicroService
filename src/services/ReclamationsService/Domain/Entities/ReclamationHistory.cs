using Savora.Shared.Enums;

namespace Savora.ReclamationsService.Domain.Entities;

public class ReclamationHistory
{
    public Guid Id { get; set; }
    public Guid ReclamationId { get; set; }
    public ReclamationStatus? OldStatus { get; set; }
    public ReclamationStatus? NewStatus { get; set; }
    public Priority? OldPriority { get; set; }
    public Priority? NewPriority { get; set; }
    public string ActionType { get; set; } = string.Empty; // StatusChange, PriorityChange, Created, Comment
    public string? Comment { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Reclamation Reclamation { get; set; } = null!;
}

