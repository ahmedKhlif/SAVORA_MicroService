using Savora.Shared.Enums;

namespace Savora.InterventionsService.Domain.Entities;

public class Intervention
{
    public Guid Id { get; set; }
    public Guid ReclamationId { get; set; }
    public Guid? TechnicianId { get; set; }
    public InterventionStatus Status { get; set; } = InterventionStatus.Planned;
    public DateTime PlannedDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public string? DiagnosticNotes { get; set; }
    public string? ResolutionNotes { get; set; }
    public bool IsFree { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Technician? Technician { get; set; }
    public ICollection<PartUsed> PartsUsed { get; set; } = new List<PartUsed>();
    public Labor? Labor { get; set; }
    public Invoice? Invoice { get; set; }

    // Computed
    public decimal TotalPartsAmount => PartsUsed.Sum(p => p.TotalPrice);
    public decimal TotalLaborAmount => Labor?.TotalAmount ?? 0;
    public decimal TotalAmount => IsFree ? 0 : TotalPartsAmount + TotalLaborAmount;
}

