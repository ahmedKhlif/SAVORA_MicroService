namespace Savora.ArticlesService.Domain.Entities;

public class Article
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public int WarrantyMonths { get; set; }
    public Guid? ClientId { get; set; } // Client who owns this article (required for SAV)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Computed properties
    public bool IsUnderWarranty
    {
        get
        {
            if (WarrantyMonths <= 0) return false;
            var warrantyEndDate = PurchaseDate.AddMonths(WarrantyMonths);
            return DateTime.UtcNow <= warrantyEndDate;
        }
    }

    public DateTime? WarrantyEndDate
    {
        get
        {
            if (WarrantyMonths <= 0) return null;
            return PurchaseDate.AddMonths(WarrantyMonths);
        }
    }
}



