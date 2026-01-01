using System.ComponentModel.DataAnnotations;

namespace Savora.Shared.DTOs.Articles;

public class PartDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; }
    public string? Category { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreatePartRequest
{
    [Required(ErrorMessage = "Reference is required")]
    public string Reference { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Unit price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
    public decimal UnitPrice { get; set; }

    [Required(ErrorMessage = "Stock quantity is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
    public int StockQuantity { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Minimum stock level cannot be negative")]
    public int MinStockLevel { get; set; } = 5;

    public string? Category { get; set; }
}

public class UpdatePartRequest
{
    [Required(ErrorMessage = "Reference is required")]
    public string Reference { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Unit price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
    public decimal UnitPrice { get; set; }

    [Required(ErrorMessage = "Stock quantity is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
    public int StockQuantity { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Minimum stock level cannot be negative")]
    public int MinStockLevel { get; set; }

    public string? Category { get; set; }
}

public class UpdateStockRequest
{
    [Required(ErrorMessage = "Quantity change is required")]
    public int QuantityChange { get; set; }

    public string? Reason { get; set; }
}

