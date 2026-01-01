using System.ComponentModel.DataAnnotations;

namespace Savora.Shared.DTOs.Articles;

public class ArticleDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime PurchaseDate { get; set; }
    public int WarrantyMonths { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string? ClientName { get; set; }
    public bool IsUnderWarranty { get; set; }
    public DateTime WarrantyEndDate { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateArticleRequest
{
    [Required(ErrorMessage = "Reference is required")]
    public string Reference { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Brand is required")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Purchase date is required")]
    public DateTime PurchaseDate { get; set; }

    [Required(ErrorMessage = "Warranty months is required")]
    [Range(1, 120, ErrorMessage = "Warranty must be between 1 and 120 months")]
    public int WarrantyMonths { get; set; }

    [Required(ErrorMessage = "Serial number is required")]
    public string SerialNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Client ID is required")]
    public Guid ClientId { get; set; }
}

public class UpdateArticleRequest
{
    [Required(ErrorMessage = "Reference is required")]
    public string Reference { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Brand is required")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Purchase date is required")]
    public DateTime PurchaseDate { get; set; }

    [Required(ErrorMessage = "Warranty months is required")]
    [Range(1, 120, ErrorMessage = "Warranty must be between 1 and 120 months")]
    public int WarrantyMonths { get; set; }

    [Required(ErrorMessage = "Serial number is required")]
    public string SerialNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Client ID is required")]
    public Guid ClientId { get; set; }
}

