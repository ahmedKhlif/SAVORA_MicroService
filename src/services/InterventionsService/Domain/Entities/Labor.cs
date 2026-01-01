namespace Savora.InterventionsService.Domain.Entities;

public class Labor
{
    public Guid Id { get; set; }
    public Guid InterventionId { get; set; }
    public decimal Hours { get; set; }
    public decimal HourlyRate { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Intervention Intervention { get; set; } = null!;

    // Computed
    public decimal TotalAmount => Hours * HourlyRate;
}

