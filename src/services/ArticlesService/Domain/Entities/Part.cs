namespace Savora.ArticlesService.Domain.Entities;

public class Part
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; } = 5;
    public string? Category { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    // Computed property
    public bool IsLowStock => StockQuantity <= MinStockLevel;
}

