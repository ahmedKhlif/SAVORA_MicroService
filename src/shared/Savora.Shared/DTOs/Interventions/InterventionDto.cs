using System.ComponentModel.DataAnnotations;
using Savora.Shared.Enums;

namespace Savora.Shared.DTOs.Interventions;

public class InterventionDto
{
    public Guid Id { get; set; }
    public Guid ReclamationId { get; set; }
    public string ReclamationTitle { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ArticleName { get; set; } = string.Empty;
    public Guid? TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public InterventionStatus Status { get; set; }
    public DateTime PlannedDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public string? DiagnosticNotes { get; set; }
    public string? ResolutionNotes { get; set; }
    public bool IsFree { get; set; }
    public List<PartUsedDto> PartsUsed { get; set; } = new();
    public List<LaborDto> Labor { get; set; } = new();
    public decimal PartsTotal { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public bool HasInvoice { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class InterventionListDto
{
    public Guid Id { get; set; }
    public Guid ReclamationId { get; set; }
    public string ReclamationTitle { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public Guid? TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public InterventionStatus Status { get; set; }
    public DateTime PlannedDate { get; set; }
    public bool IsFree { get; set; }
    public decimal TotalAmount { get; set; }
    public bool HasInvoice { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateInterventionRequest
{
    [Required(ErrorMessage = "Reclamation ID is required")]
    public Guid ReclamationId { get; set; }

    public Guid? TechnicianId { get; set; }

    [Required(ErrorMessage = "Planned date is required")]
    public DateTime PlannedDate { get; set; }

    public string? Notes { get; set; }
    
    // Client info for email notification
    public string? ClientEmail { get; set; }
    public string? ClientName { get; set; }
    public string? ReclamationTitle { get; set; }
}

public class UpdateInterventionRequest
{
    public Guid? TechnicianId { get; set; }

    [Required(ErrorMessage = "Planned date is required")]
    public DateTime PlannedDate { get; set; }

    public string? Notes { get; set; }
    public string? DiagnosticNotes { get; set; }
    public string? ResolutionNotes { get; set; }
}

public class UpdateInterventionStatusRequest
{
    [Required(ErrorMessage = "New status is required")]
    public InterventionStatus NewStatus { get; set; }

    public string? Notes { get; set; }
    
    // Client info for email notification (optional, for completion emails)
    public string? ClientEmail { get; set; }
    public string? ClientName { get; set; }
    public string? ReclamationTitle { get; set; }
}

public class PartUsedDto
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartReference { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class AddPartUsedRequest
{
    [Required(ErrorMessage = "Part ID is required")]
    public Guid PartId { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}

public class LaborDto
{
    public Guid Id { get; set; }
    public decimal Hours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Description { get; set; }
}

public class SetLaborRequest
{
    [Required(ErrorMessage = "Hours is required")]
    [Range(0.25, 100, ErrorMessage = "Hours must be between 0.25 and 100")]
    public decimal Hours { get; set; }

    [Required(ErrorMessage = "Hourly rate is required")]
    [Range(1, 1000, ErrorMessage = "Hourly rate must be between 1 and 1000")]
    public decimal HourlyRate { get; set; }

    public string? Description { get; set; }
}

public class InvoiceDto
{
    public Guid Id { get; set; }
    public Guid InterventionId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public bool IsFree { get; set; }
    public decimal PartsTotal { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PdfPath { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TechnicianDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public bool IsAvailable { get; set; }
    public int ActiveInterventions { get; set; }
    public int CompletedInterventions { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTechnicianRequest
{
    [Required(ErrorMessage = "Full name is required")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required")]
    public string Phone { get; set; } = string.Empty;

    public List<string> Skills { get; set; } = new();
}

public class UpdateTechnicianRequest
{
    [Required(ErrorMessage = "Full name is required")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required")]
    public string Phone { get; set; } = string.Empty;

    public List<string> Skills { get; set; } = new();
    public bool IsAvailable { get; set; }
}

public class AddPartRequest
{
    [Required(ErrorMessage = "Part ID is required")]
    public Guid PartId { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}

public class AddLaborRequest
{
    public string? Description { get; set; }

    [Required(ErrorMessage = "Hours is required")]
    [Range(0.5, 100, ErrorMessage = "Hours must be between 0.5 and 100")]
    public decimal Hours { get; set; }

    [Required(ErrorMessage = "Hourly rate is required")]
    [Range(1, 1000, ErrorMessage = "Hourly rate must be between 1 and 1000")]
    public decimal HourlyRate { get; set; }
}

public class InvoiceListDto
{
    public Guid Id { get; set; }
    public Guid InterventionId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ReclamationTitle { get; set; } = string.Empty;
    public bool IsFree { get; set; }
    public decimal PartsTotal { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InvoiceDetailDto
{
    public Guid Id { get; set; }
    public Guid InterventionId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public string? ClientAddress { get; set; }
    public string ReclamationTitle { get; set; } = string.Empty;
    public bool IsFree { get; set; }
    public List<PartUsedDto> Parts { get; set; } = new();
    public List<LaborDto> Labor { get; set; } = new();
    public decimal PartsTotal { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PdfPath { get; set; }
    public DateTime CreatedAt { get; set; }
}

