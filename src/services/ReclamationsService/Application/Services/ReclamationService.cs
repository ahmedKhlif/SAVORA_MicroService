using Microsoft.EntityFrameworkCore;
using Savora.ReclamationsService.Domain.Entities;
using Savora.ReclamationsService.Infrastructure.Data;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;
using Savora.Shared.Enums;

namespace Savora.ReclamationsService.Application.Services;

public class ReclamationServiceImpl : IReclamationService
{
    private readonly ReclamationsDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly IArticleApiClient _articleApiClient;
    private readonly ILogger<ReclamationServiceImpl> _logger;

    public ReclamationServiceImpl(
        ReclamationsDbContext context,
        INotificationService notificationService,
        IEmailService emailService,
        IArticleApiClient articleApiClient,
        ILogger<ReclamationServiceImpl> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _emailService = emailService;
        _articleApiClient = articleApiClient;
        _logger = logger;
    }

    public async Task<ApiResponse<ReclamationDto>> GetByIdAsync(Guid id)
    {
        var reclamation = await _context.Reclamations
            .Include(r => r.Client)
            .Include(r => r.Attachments)
            .Include(r => r.History.OrderByDescending(h => h.ChangedAt))
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reclamation == null)
        {
            return ApiResponse<ReclamationDto>.FailureResponse("Reclamation not found");
        }

        return ApiResponse<ReclamationDto>.SuccessResponse(await MapToDtoAsync(reclamation));
    }

    public async Task<ApiResponse<PaginatedResult<ReclamationListDto>>> GetAllAsync(PaginationParams pagination, ReclamationFilterParams? filter = null)
    {
        var query = _context.Reclamations
            .Include(r => r.Client)
            .Include(r => r.Attachments)
            .AsQueryable();

        // Apply filters
        if (filter != null)
        {
            if (filter.ClientId.HasValue)
                query = query.Where(r => r.ClientId == filter.ClientId.Value);

            if (filter.Status.HasValue)
                query = query.Where(r => r.Status == filter.Status.Value);

            if (filter.Priority.HasValue)
                query = query.Where(r => r.Priority == filter.Priority.Value);

            if (filter.CreatedFrom.HasValue)
                query = query.Where(r => r.CreatedAt >= filter.CreatedFrom.Value);

            if (filter.CreatedTo.HasValue)
                query = query.Where(r => r.CreatedAt <= filter.CreatedTo.Value);
        }

        // Apply search
        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            var searchLower = pagination.SearchTerm.ToLower();
            query = query.Where(r =>
                r.Title.ToLower().Contains(searchLower) ||
                r.Description.ToLower().Contains(searchLower) ||
                r.Client.FullName.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync();

        // Apply sorting
        query = pagination.SortBy?.ToLower() switch
        {
            "title" => pagination.SortDescending ? query.OrderByDescending(r => r.Title) : query.OrderBy(r => r.Title),
            "status" => pagination.SortDescending ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
            "priority" => pagination.SortDescending ? query.OrderByDescending(r => r.Priority) : query.OrderBy(r => r.Priority),
            "client" => pagination.SortDescending ? query.OrderByDescending(r => r.Client.FullName) : query.OrderBy(r => r.Client.FullName),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var dtos = items.Select(MapToListDto).ToList();

        // Filter by SLA status in memory if needed
        if (!string.IsNullOrWhiteSpace(filter?.SlaStatus))
        {
            dtos = dtos.Where(d => d.SlaStatus == filter.SlaStatus).ToList();
        }

        var result = new PaginatedResult<ReclamationListDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize);
        return ApiResponse<PaginatedResult<ReclamationListDto>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<List<ReclamationListDto>>> GetByClientIdAsync(Guid clientId)
    {
        var reclamations = await _context.Reclamations
            .Include(r => r.Client)
            .Include(r => r.Attachments)
            .Where(r => r.ClientId == clientId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var dtos = reclamations.Select(MapToListDto).ToList();
        return ApiResponse<List<ReclamationListDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<ReclamationDto>> CreateAsync(CreateReclamationRequest request, Guid clientId, string createdBy)
    {
        // Get client info for email
        var client = await _context.Clients.FindAsync(clientId);
        
        var reclamation = new Reclamation
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ClientArticleId = request.ClientArticleId,
            Title = request.Title,
            Description = request.Description,
            Priority = Priority.Medium,
            Status = ReclamationStatus.New,
            CreatedAt = DateTime.UtcNow
        };

        reclamation.SetSlaDeadline();

        _context.Reclamations.Add(reclamation);

        // Add creation history
        var history = new ReclamationHistory
        {
            Id = Guid.NewGuid(),
            ReclamationId = reclamation.Id,
            NewStatus = ReclamationStatus.New,
            ActionType = "Created",
            Comment = "Réclamation créée",
            ChangedBy = createdBy,
            ChangedAt = DateTime.UtcNow
        };
        _context.ReclamationHistories.Add(history);

        await _context.SaveChangesAsync();

        // Send notification to SAV
        await _notificationService.CreateAsync(new CreateNotificationRequest
        {
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"), // SAV admin
            Title = "Nouvelle réclamation",
            Message = $"Une nouvelle réclamation a été créée: {request.Title}",
            NotificationType = "ReclamationCreated",
            RelatedEntityId = reclamation.Id,
            RelatedEntityType = "Reclamation"
        });

        // Send email to client
        if (client != null && !string.IsNullOrEmpty(client.Email))
        {
            try
            {
                await _emailService.SendReclamationCreatedEmailAsync(
                    client.Email,
                    client.FullName,
                    request.Title,
                    reclamation.Id.ToString()
                );
                _logger.LogInformation("Email sent to client {ClientEmail} for reclamation {ReclamationId}", client.Email, reclamation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email to client for reclamation {ReclamationId}", reclamation.Id);
            }
        }

        _logger.LogInformation("Reclamation {ReclamationId} created by {CreatedBy}", reclamation.Id, createdBy);

        return await GetByIdAsync(reclamation.Id);
    }

    public async Task<ApiResponse<ReclamationDto>> UpdateAsync(Guid id, UpdateReclamationRequest request, string updatedBy)
    {
        var reclamation = await _context.Reclamations.FindAsync(id);
        if (reclamation == null)
        {
            return ApiResponse<ReclamationDto>.FailureResponse("Reclamation not found");
        }

        reclamation.Title = request.Title;
        reclamation.Description = request.Description;
        reclamation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reclamation {ReclamationId} updated by {UpdatedBy}", id, updatedBy);
        return await GetByIdAsync(id);
    }

    public async Task<ApiResponse<ReclamationDto>> UpdateStatusAsync(Guid id, UpdateReclamationStatusRequest request, string changedBy, Guid? changedByUserId = null)
    {
        var reclamation = await _context.Reclamations
            .Include(r => r.Client)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reclamation == null)
        {
            return ApiResponse<ReclamationDto>.FailureResponse("Reclamation not found");
        }

        var oldStatus = reclamation.Status;
        reclamation.Status = request.NewStatus;
        reclamation.UpdatedAt = DateTime.UtcNow;

        if (request.NewStatus == ReclamationStatus.Closed)
        {
            reclamation.ClosedBy = changedBy;
            reclamation.ClosedAt = DateTime.UtcNow;
        }

        // Add history
        var history = new ReclamationHistory
        {
            Id = Guid.NewGuid(),
            ReclamationId = reclamation.Id,
            OldStatus = oldStatus,
            NewStatus = request.NewStatus,
            ActionType = "StatusChange",
            Comment = request.Comment,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow
        };
        _context.ReclamationHistories.Add(history);

        await _context.SaveChangesAsync();

        // Notify client (if different from the person changing the status)
        try
        {
            if (reclamation.Client != null && reclamation.Client.UserId != Guid.Empty)
            {
                // Only notify client if they are not the one making the change
                if (changedByUserId == null || reclamation.Client.UserId != changedByUserId.Value)
                {
                    var oldStatusLabel = GetStatusLabel(oldStatus);
                    var newStatusLabel = GetStatusLabel(request.NewStatus);
                    
                    await _notificationService.CreateAsync(new CreateNotificationRequest
                    {
                        UserId = reclamation.Client.UserId,
                        Title = "Statut de réclamation mis à jour",
                        Message = $"Le statut de votre réclamation '{reclamation.Title}' a été mis à jour de '{oldStatusLabel}' à '{newStatusLabel}'",
                        NotificationType = "StatusChanged",
                        RelatedEntityId = reclamation.Id,
                        RelatedEntityType = "Reclamation"
                    });
                    _logger.LogInformation("Notification created for client {ClientUserId} about reclamation {ReclamationId} status change", 
                        reclamation.Client.UserId, reclamation.Id);
                }
                else
                {
                    _logger.LogInformation("Skipping notification: Client {ClientUserId} is the one changing the status for reclamation {ReclamationId}", 
                        reclamation.Client.UserId, reclamation.Id);
                }
            }
            else
            {
                _logger.LogWarning("Cannot create notification: Client is null or UserId is empty for reclamation {ReclamationId}", reclamation.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for reclamation {ReclamationId} status change", reclamation.Id);
        }

        // Send email to client about status change
        if (reclamation.Client != null && !string.IsNullOrEmpty(reclamation.Client.Email))
        {
            try
            {
                await _emailService.SendReclamationStatusChangedEmailAsync(
                    reclamation.Client.Email,
                    reclamation.Client.FullName,
                    reclamation.Title,
                    GetStatusLabel(oldStatus),
                    GetStatusLabel(request.NewStatus)
                );
                _logger.LogInformation("Status change email sent to {ClientEmail}", reclamation.Client.Email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send status change email for reclamation {ReclamationId}", reclamation.Id);
            }
        }

        _logger.LogInformation("Reclamation {ReclamationId} status changed from {OldStatus} to {NewStatus} by {ChangedBy}",
            id, oldStatus, request.NewStatus, changedBy);

        return await GetByIdAsync(id);
    }

    private static string GetStatusLabel(ReclamationStatus status) => status switch
    {
        ReclamationStatus.New => "Nouvelle",
        ReclamationStatus.InProgress => "En cours",
        ReclamationStatus.PendingIntervention => "En attente d'intervention",
        ReclamationStatus.InterventionScheduled => "Intervention planifiée",
        ReclamationStatus.UnderIntervention => "En intervention",
        ReclamationStatus.PendingInvoice => "En attente de facture",
        ReclamationStatus.Closed => "Clôturée",
        ReclamationStatus.Cancelled => "Annulée",
        _ => status.ToString()
    };

    public async Task<ApiResponse<ReclamationDto>> UpdatePriorityAsync(Guid id, UpdateReclamationPriorityRequest request, string changedBy)
    {
        var reclamation = await _context.Reclamations.FindAsync(id);
        if (reclamation == null)
        {
            return ApiResponse<ReclamationDto>.FailureResponse("Reclamation not found");
        }

        var oldPriority = reclamation.Priority;
        reclamation.Priority = request.Priority;
        reclamation.UpdatedAt = DateTime.UtcNow;
        reclamation.SetSlaDeadline(); // Recalculate SLA based on new priority

        // Add history
        var history = new ReclamationHistory
        {
            Id = Guid.NewGuid(),
            ReclamationId = reclamation.Id,
            OldPriority = oldPriority,
            NewPriority = request.Priority,
            ActionType = "PriorityChange",
            Comment = request.Comment,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow
        };
        _context.ReclamationHistories.Add(history);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reclamation {ReclamationId} priority changed from {OldPriority} to {NewPriority} by {ChangedBy}",
            id, oldPriority, request.Priority, changedBy);

        return await GetByIdAsync(id);
    }

    public async Task<ApiResponse> DeleteAsync(Guid id, string deletedBy)
    {
        var reclamation = await _context.Reclamations.FindAsync(id);
        if (reclamation == null)
        {
            return ApiResponse.FailureResponse("Reclamation not found");
        }

        reclamation.IsDeleted = true;
        reclamation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Reclamation {ReclamationId} soft deleted by {DeletedBy}", id, deletedBy);
        return ApiResponse.SuccessResponse("Reclamation deleted successfully");
    }

    public async Task<ApiResponse> RestoreAsync(Guid id)
    {
        var reclamation = await _context.Reclamations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reclamation == null)
        {
            return ApiResponse.FailureResponse("Reclamation not found");
        }

        reclamation.IsDeleted = false;
        reclamation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Reclamation {ReclamationId} restored", id);
        return ApiResponse.SuccessResponse("Reclamation restored successfully");
    }

    public async Task<ApiResponse<ReclamationDto>> CloseAsync(Guid id, string closedBy, string? comment = null)
    {
        return await UpdateStatusAsync(id, new UpdateReclamationStatusRequest
        {
            NewStatus = ReclamationStatus.Closed,
            Comment = comment ?? "Réclamation clôturée"
        }, closedBy);
    }

    private async Task<ReclamationDto> MapToDtoAsync(Reclamation reclamation)
    {
        var dto = new ReclamationDto
        {
            Id = reclamation.Id,
            ClientId = reclamation.ClientId,
            ClientName = reclamation.Client.FullName,
            ClientPhone = reclamation.Client.Phone,
            ClientEmail = reclamation.Client.Email,
            ClientArticleId = reclamation.ClientArticleId,
            Title = reclamation.Title,
            Description = reclamation.Description,
            Priority = reclamation.Priority,
            Status = reclamation.Status,
            SlaDeadline = reclamation.SlaDeadline,
            SlaStatus = reclamation.GetSlaStatus(),
            Attachments = reclamation.Attachments.Select(a => new AttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                Size = a.Size,
                Url = a.Path,
                UploadedAt = a.UploadedAt
            }).ToList(),
            History = reclamation.History.Select(h => new ReclamationHistoryDto
            {
                Id = h.Id,
                OldStatus = h.OldStatus,
                NewStatus = h.NewStatus,
                OldPriority = h.OldPriority,
                NewPriority = h.NewPriority,
                ActionType = h.ActionType,
                Comment = h.Comment,
                ChangedBy = h.ChangedBy,
                ChangedAt = h.ChangedAt
            }).ToList(),
            IsDeleted = reclamation.IsDeleted,
            CreatedAt = reclamation.CreatedAt,
            UpdatedAt = reclamation.UpdatedAt,
            ClosedBy = reclamation.ClosedBy,
            ClosedAt = reclamation.ClosedAt
        };

        // Fetch article information
        if (reclamation.ClientArticleId != Guid.Empty)
        {
            try
            {
                var article = await _articleApiClient.GetArticleAsync(reclamation.ClientArticleId);
                if (article != null)
                {
                    dto.ArticleName = article.Name;
                    dto.ArticleReference = article.Reference;
                    dto.IsUnderWarranty = article.IsUnderWarranty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch article {ArticleId} for reclamation {ReclamationId}", 
                    reclamation.ClientArticleId, reclamation.Id);
            }
        }

        return dto;
    }

    private ReclamationDto MapToDto(Reclamation reclamation)
    {
        // Synchronous version for backward compatibility
        // Note: ArticleName and ArticleReference will be empty in this case
        return new ReclamationDto
        {
            Id = reclamation.Id,
            ClientId = reclamation.ClientId,
            ClientName = reclamation.Client.FullName,
            ClientPhone = reclamation.Client.Phone,
            ClientEmail = reclamation.Client.Email,
            ClientArticleId = reclamation.ClientArticleId,
            Title = reclamation.Title,
            Description = reclamation.Description,
            Priority = reclamation.Priority,
            Status = reclamation.Status,
            SlaDeadline = reclamation.SlaDeadline,
            SlaStatus = reclamation.GetSlaStatus(),
            Attachments = reclamation.Attachments.Select(a => new AttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                Size = a.Size,
                Url = a.Path,
                UploadedAt = a.UploadedAt
            }).ToList(),
            History = reclamation.History.Select(h => new ReclamationHistoryDto
            {
                Id = h.Id,
                OldStatus = h.OldStatus,
                NewStatus = h.NewStatus,
                OldPriority = h.OldPriority,
                NewPriority = h.NewPriority,
                ActionType = h.ActionType,
                Comment = h.Comment,
                ChangedBy = h.ChangedBy,
                ChangedAt = h.ChangedAt
            }).ToList(),
            IsDeleted = reclamation.IsDeleted,
            CreatedAt = reclamation.CreatedAt,
            UpdatedAt = reclamation.UpdatedAt,
            ClosedBy = reclamation.ClosedBy,
            ClosedAt = reclamation.ClosedAt
        };
    }

    private ReclamationListDto MapToListDto(Reclamation reclamation)
    {
        return new ReclamationListDto
        {
            Id = reclamation.Id,
            ClientName = reclamation.Client.FullName,
            Title = reclamation.Title,
            Priority = reclamation.Priority,
            Status = reclamation.Status,
            SlaStatus = reclamation.GetSlaStatus(),
            AttachmentCount = reclamation.Attachments.Count,
            CreatedAt = reclamation.CreatedAt
        };
    }
}

