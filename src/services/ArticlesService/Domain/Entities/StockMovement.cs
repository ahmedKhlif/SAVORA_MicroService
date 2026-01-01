namespace Savora.ArticlesService.Domain.Entities;

public class StockMovement
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public int QuantityChange { get; set; }
    public int PreviousQuantity { get; set; }
    public int NewQuantity { get; set; }
    public string MovementType { get; set; } = string.Empty; // IN, OUT, ADJUSTMENT
    public string? Reason { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Part Part { get; set; } = null!;
}

