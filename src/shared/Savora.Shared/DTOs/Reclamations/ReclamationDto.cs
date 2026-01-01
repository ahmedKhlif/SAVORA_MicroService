using System.ComponentModel.DataAnnotations;
using Savora.Shared.Enums;

namespace Savora.Shared.DTOs.Reclamations;

public class ReclamationDto
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public Guid ClientArticleId { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public string ArticleReference { get; set; } = string.Empty;
    public bool IsUnderWarranty { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public ReclamationStatus Status { get; set; }
    public DateTime? SlaDeadline { get; set; }
    public string SlaStatus { get; set; } = "OnTime"; // OnTime, NearDeadline, Overdue
    public List<AttachmentDto> Attachments { get; set; } = new();
    public List<ReclamationHistoryDto> History { get; set; } = new();
    public int InterventionCount { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public class ReclamationListDto
{
    public Guid Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ArticleName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public ReclamationStatus Status { get; set; }
    public bool IsUnderWarranty { get; set; }
    public string SlaStatus { get; set; } = "OnTime";
    public int AttachmentCount { get; set; }
    public int InterventionCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReclamationRequest
{
    [Required(ErrorMessage = "Client Article ID is required")]
    public Guid ClientArticleId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [MinLength(5, ErrorMessage = "Title must be at least 5 characters")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [MinLength(20, ErrorMessage = "Description must be at least 20 characters")]
    public string Description { get; set; } = string.Empty;
}

public class UpdateReclamationRequest
{
    [Required(ErrorMessage = "Title is required")]
    [MinLength(5, ErrorMessage = "Title must be at least 5 characters")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [MinLength(20, ErrorMessage = "Description must be at least 20 characters")]
    public string Description { get; set; } = string.Empty;
}

public class UpdateReclamationStatusRequest
{
    [Required(ErrorMessage = "New status is required")]
    public ReclamationStatus NewStatus { get; set; }

    public string? Comment { get; set; }
}

public class UpdateReclamationPriorityRequest
{
    [Required(ErrorMessage = "Priority is required")]
    public Priority Priority { get; set; }

    public string? Comment { get; set; }
}

public class AttachmentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public class ReclamationHistoryDto
{
    public Guid Id { get; set; }
    public ReclamationStatus? OldStatus { get; set; }
    public ReclamationStatus? NewStatus { get; set; }
    public Priority? OldPriority { get; set; }
    public Priority? NewPriority { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}

