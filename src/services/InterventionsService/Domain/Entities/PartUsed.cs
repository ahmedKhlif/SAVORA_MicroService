namespace Savora.InterventionsService.Domain.Entities;

public class PartUsed
{
    public Guid Id { get; set; }
    public Guid InterventionId { get; set; }
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartReference { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Intervention Intervention { get; set; } = null!;

    // Computed
    public decimal TotalPrice => Quantity * UnitPriceSnapshot;
}

