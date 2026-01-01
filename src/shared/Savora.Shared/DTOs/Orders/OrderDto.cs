using System.ComponentModel.DataAnnotations;
using Savora.Shared.Enums;

namespace Savora.Shared.DTOs.Orders;

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public Guid ArticleId { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty; // Alias for ArticleName
    public string ArticleReference { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public string? Notes { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

public class OrderListDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public Guid ArticleId { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty; // Alias for ArticleName
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateOrderRequest
{
    [Required(ErrorMessage = "Article ID is required")]
    public Guid ArticleId { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
    public int Quantity { get; set; } = 1;

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, 1000000, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    public string? Notes { get; set; }
}

public class UpdateOrderStatusRequest
{
    [Required(ErrorMessage = "New status is required")]
    public OrderStatus NewStatus { get; set; }

    public string? Notes { get; set; }
}


