namespace Savora.InterventionsService.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid? InterventionId { get; set; } // Nullable for order invoices
    public Guid? OrderId { get; set; } // Nullable for intervention invoices
    public string InvoiceNumber { get; set; } = string.Empty;
    public bool IsFree { get; set; }
    public decimal PartsTotal { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PdfPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Intervention? Intervention { get; set; }
}

