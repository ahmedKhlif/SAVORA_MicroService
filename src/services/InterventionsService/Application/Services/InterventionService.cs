using Microsoft.EntityFrameworkCore;
using Savora.InterventionsService.Domain.Entities;
using Savora.InterventionsService.Infrastructure.Data;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Interventions;
using Savora.Shared.DTOs.Reclamations;
using Savora.Shared.Enums;

namespace Savora.InterventionsService.Application.Services;

public class InterventionServiceImpl : IInterventionService
{
    private readonly InterventionsDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IReclamationApiClient _reclamationApiClient;
    private readonly INotificationApiClient _notificationApiClient;
    private readonly IClientApiClient _clientApiClient;
    private readonly ILogger<InterventionServiceImpl> _logger;

    public InterventionServiceImpl(
        InterventionsDbContext context, 
        IEmailService emailService,
        IReclamationApiClient reclamationApiClient,
        INotificationApiClient notificationApiClient,
        IClientApiClient clientApiClient,
        ILogger<InterventionServiceImpl> logger)
    {
        _context = context;
        _emailService = emailService;
        _reclamationApiClient = reclamationApiClient;
        _notificationApiClient = notificationApiClient;
        _clientApiClient = clientApiClient;
        _logger = logger;
    }

    public async Task<ApiResponse<InterventionDto>> GetByIdAsync(Guid id)
    {
        var intervention = await _context.Interventions
            .Include(i => i.Technician)
            .Include(i => i.PartsUsed)
            .Include(i => i.Labor)
            .Include(i => i.Invoice)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (intervention == null)
        {
            return ApiResponse<InterventionDto>.FailureResponse("Intervention not found");
        }

        // Fetch reclamation data
        ReclamationDto? reclamation = null;
        try
        {
            reclamation = await _reclamationApiClient.GetReclamationAsync(intervention.ReclamationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch reclamation {ReclamationId} for intervention {InterventionId}", intervention.ReclamationId, id);
        }

        return ApiResponse<InterventionDto>.SuccessResponse(MapToDto(intervention, reclamation));
    }

    public async Task<ApiResponse<PaginatedResult<InterventionListDto>>> GetAllAsync(PaginationParams pagination, InterventionFilterParams? filter = null)
    {
        var query = _context.Interventions
            .Include(i => i.Technician)
            .Include(i => i.Invoice)
            .Include(i => i.PartsUsed)
            .Include(i => i.Labor)
            .AsQueryable();

        // Apply filters
        if (filter != null)
        {
            if (filter.ReclamationId.HasValue)
                query = query.Where(i => i.ReclamationId == filter.ReclamationId.Value);

            if (filter.TechnicianId.HasValue)
                query = query.Where(i => i.TechnicianId == filter.TechnicianId.Value);

            if (filter.Status.HasValue)
                query = query.Where(i => i.Status == filter.Status.Value);

            if (filter.IsFree.HasValue)
                query = query.Where(i => i.IsFree == filter.IsFree.Value);

            if (filter.PlannedDateFrom.HasValue)
                query = query.Where(i => i.PlannedDate >= filter.PlannedDateFrom.Value);

            if (filter.PlannedDateTo.HasValue)
                query = query.Where(i => i.PlannedDate <= filter.PlannedDateTo.Value);
        }

        var totalCount = await query.CountAsync();

        // Apply sorting
        query = pagination.SortBy?.ToLower() switch
        {
            "planneddate" => pagination.SortDescending ? query.OrderByDescending(i => i.PlannedDate) : query.OrderBy(i => i.PlannedDate),
            "status" => pagination.SortDescending ? query.OrderByDescending(i => i.Status) : query.OrderBy(i => i.Status),
            _ => query.OrderByDescending(i => i.CreatedAt)
        };

        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        // Enrich with reclamation data
        var reclamationIds = items.Select(i => i.ReclamationId).Distinct().ToList();
        var reclamationsDict = new Dictionary<Guid, (string Title, string ClientName)>();
        
        foreach (var reclamationId in reclamationIds)
        {
            try
            {
                var reclamation = await _reclamationApiClient.GetReclamationAsync(reclamationId);
                if (reclamation != null)
                {
                    reclamationsDict[reclamationId] = (reclamation.Title, reclamation.ClientName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch reclamation {ReclamationId} for intervention list", reclamationId);
            }
        }

        var dtos = items.Select(i => MapToListDto(i, reclamationsDict)).ToList();
        var result = new PaginatedResult<InterventionListDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize);
        return ApiResponse<PaginatedResult<InterventionListDto>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<List<InterventionListDto>>> GetByReclamationIdAsync(Guid reclamationId)
    {
        var interventions = await _context.Interventions
            .Include(i => i.Technician)
            .Include(i => i.Invoice)
            .Include(i => i.PartsUsed)
            .Include(i => i.Labor)
            .Where(i => i.ReclamationId == reclamationId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        // Enrich with reclamation data
        var reclamationsDict = new Dictionary<Guid, (string Title, string ClientName)>();
        try
        {
            var reclamation = await _reclamationApiClient.GetReclamationAsync(reclamationId);
            if (reclamation != null)
            {
                reclamationsDict[reclamationId] = (reclamation.Title, reclamation.ClientName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch reclamation {ReclamationId} for intervention list", reclamationId);
        }

        var dtos = interventions.Select(i => MapToListDto(i, reclamationsDict)).ToList();
        return ApiResponse<List<InterventionListDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<List<InterventionListDto>>> GetByTechnicianIdAsync(Guid technicianId)
    {
        var interventions = await _context.Interventions
            .Include(i => i.Technician)
            .Include(i => i.Invoice)
            .Include(i => i.PartsUsed)
            .Include(i => i.Labor)
            .Where(i => i.TechnicianId == technicianId)
            .OrderByDescending(i => i.PlannedDate)
            .ToListAsync();

        // Enrich with reclamation data
        var reclamationIds = interventions.Select(i => i.ReclamationId).Distinct().ToList();
        var reclamationsDict = new Dictionary<Guid, (string Title, string ClientName)>();
        
        foreach (var reclamationId in reclamationIds)
        {
            try
            {
                var reclamation = await _reclamationApiClient.GetReclamationAsync(reclamationId);
                if (reclamation != null)
                {
                    reclamationsDict[reclamationId] = (reclamation.Title, reclamation.ClientName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch reclamation {ReclamationId} for intervention list", reclamationId);
            }
        }

        var dtos = interventions.Select(i => MapToListDto(i, reclamationsDict)).ToList();
        return ApiResponse<List<InterventionListDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<InterventionDto>> CreateAsync(CreateInterventionRequest request, bool isFree)
    {
        // Get technician name if assigned
        string? technicianName = null;
        if (request.TechnicianId.HasValue)
        {
            var technician = await _context.Technicians.FindAsync(request.TechnicianId.Value);
            technicianName = technician?.FullName;
        }

        var intervention = new Intervention
        {
            Id = Guid.NewGuid(),
            ReclamationId = request.ReclamationId,
            TechnicianId = request.TechnicianId,
            Status = InterventionStatus.Planned,
            PlannedDate = request.PlannedDate,
            Notes = request.Notes,
            IsFree = isFree,
            CreatedAt = DateTime.UtcNow
        };

        _context.Interventions.Add(intervention);
        await _context.SaveChangesAsync();

        // Get reclamation info for notifications
        ReclamationDto? reclamation = null;
        Guid? clientUserId = null;
        try
        {
            reclamation = await _reclamationApiClient.GetReclamationAsync(request.ReclamationId);
            if (reclamation != null)
            {
                // Get client UserId using ClientApiClient
                var client = await _clientApiClient.GetClientByIdAsync(reclamation.ClientId);
                if (client != null && client.UserId != Guid.Empty)
                {
                    clientUserId = client.UserId;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get reclamation info for notification");
        }

        // Send notification to client
        if (reclamation != null && clientUserId.HasValue)
        {
            try
            {
                await _notificationApiClient.CreateAsync(new CreateNotificationRequest
                {
                    UserId = clientUserId.Value,
                    Title = "Intervention planifiée",
                    Message = $"Une intervention a été planifiée pour votre réclamation '{reclamation.Title}' le {request.PlannedDate:dd/MM/yyyy}",
                    NotificationType = "NewIntervention",
                    RelatedEntityId = intervention.Id,
                    RelatedEntityType = "Intervention"
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create notification for intervention creation");
            }
        }

        // Send notification to technician if assigned
        if (request.TechnicianId.HasValue)
        {
            try
            {
                var technician = await _context.Technicians.FindAsync(request.TechnicianId.Value);
                if (technician != null && technician.UserId.HasValue && technician.UserId.Value != Guid.Empty)
                {
                    await _notificationApiClient.CreateAsync(new CreateNotificationRequest
                    {
                        UserId = technician.UserId.Value,
                        Title = "Nouvelle intervention assignée",
                        Message = $"Une nouvelle intervention a été assignée pour le {request.PlannedDate:dd/MM/yyyy}",
                        NotificationType = "InterventionAssigned",
                        RelatedEntityId = intervention.Id,
                        RelatedEntityType = "Intervention"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create notification for technician");
            }
        }

        // Send email to client about scheduled intervention
        if (!string.IsNullOrEmpty(request.ClientEmail))
        {
            try
            {
                await _emailService.SendInterventionScheduledEmailAsync(
                    request.ClientEmail,
                    request.ClientName ?? "Client",
                    request.ReclamationTitle ?? "Votre réclamation",
                    request.PlannedDate,
                    technicianName
                );
                _logger.LogInformation("Intervention scheduled email sent to {Email}", request.ClientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send intervention scheduled email");
            }
        }

        _logger.LogInformation("Intervention {InterventionId} created for reclamation {ReclamationId}", intervention.Id, request.ReclamationId);
        return await GetByIdAsync(intervention.Id);
    }

    public async Task<ApiResponse<InterventionDto>> UpdateAsync(Guid id, UpdateInterventionRequest request)
    {
        var intervention = await _context.Interventions.FindAsync(id);
        if (intervention == null)
        {
            return ApiResponse<InterventionDto>.FailureResponse("Intervention not found");
        }

        intervention.TechnicianId = request.TechnicianId;
        intervention.PlannedDate = request.PlannedDate;
        intervention.Notes = request.Notes;
        intervention.DiagnosticNotes = request.DiagnosticNotes;
        intervention.ResolutionNotes = request.ResolutionNotes;
        intervention.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Intervention {InterventionId} updated", id);
        return await GetByIdAsync(id);
    }

    public async Task<ApiResponse<InterventionDto>> UpdateStatusAsync(Guid id, UpdateInterventionStatusRequest request)
    {
        var intervention = await _context.Interventions
            .Include(i => i.PartsUsed)
            .Include(i => i.Labor)
            .FirstOrDefaultAsync(i => i.Id == id);
            
        if (intervention == null)
        {
            return ApiResponse<InterventionDto>.FailureResponse("Intervention not found");
        }

        var oldStatus = intervention.Status;
        intervention.Status = request.NewStatus;
        intervention.UpdatedAt = DateTime.UtcNow;

        if (request.NewStatus == InterventionStatus.InProgress && !intervention.StartedAt.HasValue)
        {
            intervention.StartedAt = DateTime.UtcNow;
        }

        if (request.NewStatus == InterventionStatus.Completed)
        {
            intervention.CompletedAt = DateTime.UtcNow;
        }

        if (!string.IsNullOrEmpty(request.Notes))
        {
            intervention.Notes = (intervention.Notes ?? "") + "\n" + request.Notes;
        }

        await _context.SaveChangesAsync();

        // Get reclamation info for notifications
        ReclamationDto? reclamation = null;
        Guid? clientUserId = null;
        try
        {
            reclamation = await _reclamationApiClient.GetReclamationAsync(intervention.ReclamationId);
            if (reclamation != null)
            {
                // Get client UserId using ClientApiClient
                var client = await _clientApiClient.GetClientByIdAsync(reclamation.ClientId);
                if (client != null && client.UserId != Guid.Empty)
                {
                    clientUserId = client.UserId;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get reclamation info for notification");
        }

        // Create notification based on status change
        if (reclamation != null && clientUserId.HasValue)
        {
            try
            {
                string title = "";
                string message = "";
                string notificationType = "";

                switch (request.NewStatus)
                {
                    case InterventionStatus.InProgress:
                        title = "Intervention démarrée";
                        message = $"L'intervention pour votre réclamation '{reclamation.Title}' a été démarrée";
                        notificationType = "InterventionStarted";
                        break;
                    case InterventionStatus.Completed:
                        var totalAmount = intervention.IsFree ? 0 : 
                            (intervention.PartsUsed?.Sum(p => p.Quantity * p.UnitPriceSnapshot) ?? 0) +
                            (intervention.Labor?.TotalAmount ?? 0);
                        title = "Intervention terminée";
                        message = intervention.IsFree 
                            ? $"L'intervention pour votre réclamation '{reclamation.Title}' a été terminée (gratuite)"
                            : $"L'intervention pour votre réclamation '{reclamation.Title}' a été terminée. Montant: {totalAmount:N2} TND";
                        notificationType = "InterventionCompleted";
                        break;
                    case InterventionStatus.Cancelled:
                        title = "Intervention annulée";
                        message = $"L'intervention pour votre réclamation '{reclamation.Title}' a été annulée";
                        notificationType = "InterventionCancelled";
                        break;
                }

                if (!string.IsNullOrEmpty(title))
                {
                    await _notificationApiClient.CreateAsync(new CreateNotificationRequest
                    {
                        UserId = clientUserId.Value,
                        Title = title,
                        Message = message,
                        NotificationType = notificationType,
                        RelatedEntityId = intervention.Id,
                        RelatedEntityType = "Intervention"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create notification for intervention status change");
            }
        }

        // Send email notification for important status changes
        await SendStatusChangeEmailAsync(intervention, oldStatus, request.NewStatus, request);

        _logger.LogInformation("Intervention {InterventionId} status changed from {OldStatus} to {NewStatus}", id, oldStatus, request.NewStatus);
        return await GetByIdAsync(id);
    }

    private async Task SendStatusChangeEmailAsync(Intervention intervention, InterventionStatus oldStatus, InterventionStatus newStatus, UpdateInterventionStatusRequest request)
    {
        // Get client info from reclamation if not provided
        string? clientEmail = request.ClientEmail;
        string? clientName = request.ClientName;
        string? reclamationTitle = request.ReclamationTitle;

        if (string.IsNullOrEmpty(clientEmail) || string.IsNullOrEmpty(clientName))
        {
            try
            {
                var reclamation = await _reclamationApiClient.GetReclamationAsync(intervention.ReclamationId);
                if (reclamation != null)
                {
                    clientEmail ??= reclamation.ClientEmail;
                    clientName ??= reclamation.ClientName;
                    reclamationTitle ??= reclamation.Title;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get reclamation info for email notification");
            }
        }

        if (string.IsNullOrEmpty(clientEmail))
        {
            _logger.LogInformation("No client email available for intervention {InterventionId} status change notification", intervention.Id);
            return;
        }

        try
        {
            // Send email based on new status
            switch (newStatus)
            {
                case InterventionStatus.InProgress:
                    // Email already sent when intervention is scheduled, but we can send a reminder
                    _logger.LogInformation("Intervention {InterventionId} started - email notification skipped (already sent on scheduling)", intervention.Id);
                    break;

                case InterventionStatus.Completed:
                    var totalAmount = intervention.IsFree ? 0 : 
                        (intervention.PartsUsed?.Sum(p => p.Quantity * p.UnitPriceSnapshot) ?? 0) +
                        (intervention.Labor?.TotalAmount ?? 0);
                    
                    await _emailService.SendInterventionCompletedEmailAsync(
                        clientEmail,
                        clientName ?? "Client",
                        reclamationTitle ?? "Votre réclamation",
                        intervention.IsFree,
                        totalAmount
                    );
                    _logger.LogInformation("Intervention completed email sent to {Email}", clientEmail);
                    break;

                case InterventionStatus.Cancelled:
                    // Could add a cancellation email here if needed
                    _logger.LogInformation("Intervention {InterventionId} cancelled - email notification could be added", intervention.Id);
                    break;

                default:
                    _logger.LogInformation("Intervention {InterventionId} status changed to {Status} - no email notification configured", intervention.Id, newStatus);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send status change email for intervention {InterventionId}", intervention.Id);
        }
    }

    public async Task<ApiResponse<InterventionDto>> AssignTechnicianAsync(Guid id, Guid technicianId)
    {
        var intervention = await _context.Interventions.FindAsync(id);
        if (intervention == null)
        {
            return ApiResponse<InterventionDto>.FailureResponse("Intervention not found");
        }

        var technician = await _context.Technicians.FindAsync(technicianId);
        if (technician == null)
        {
            return ApiResponse<InterventionDto>.FailureResponse("Technician not found");
        }

        intervention.TechnicianId = technicianId;
        intervention.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Notify technician
        if (technician.UserId.HasValue && technician.UserId.Value != Guid.Empty)
        {
            try
            {
                await _notificationApiClient.CreateAsync(new CreateNotificationRequest
                {
                    UserId = technician.UserId.Value,
                    Title = "Intervention assignée",
                    Message = $"Une intervention vous a été assignée pour le {intervention.PlannedDate:dd/MM/yyyy}",
                    NotificationType = "InterventionAssigned",
                    RelatedEntityId = intervention.Id,
                    RelatedEntityType = "Intervention"
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create notification for technician assignment");
            }
        }

        // Notify client
        try
        {
            var reclamation = await _reclamationApiClient.GetReclamationAsync(intervention.ReclamationId);
            if (reclamation != null)
            {
                var client = await _clientApiClient.GetClientByIdAsync(reclamation.ClientId);
                if (client != null && client.UserId != Guid.Empty)
                {
                    await _notificationApiClient.CreateAsync(new CreateNotificationRequest
                    {
                        UserId = client.UserId,
                        Title = "Technicien assigné",
                        Message = $"Un technicien ({technician.FullName}) a été assigné à l'intervention pour votre réclamation '{reclamation.Title}'",
                        NotificationType = "InterventionAssigned",
                        RelatedEntityId = intervention.Id,
                        RelatedEntityType = "Intervention"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create notification for client about technician assignment");
        }

        _logger.LogInformation("Technician {TechnicianId} assigned to intervention {InterventionId}", technicianId, id);
        return await GetByIdAsync(id);
    }

    public async Task<ApiResponse<InterventionDto>> AddPartUsedAsync(Guid id, AddPartUsedRequest request, string partName, string partReference, decimal unitPrice)
    {
        var intervention = await _context.Interventions.FindAsync(id);
        if (intervention == null)
        {
            return ApiResponse<InterventionDto>.FailureResponse("Intervention not found");
        }

        var partUsed = new PartUsed
        {
            Id = Guid.NewGuid(),
            InterventionId = id,
            PartId = request.PartId,
            PartName = partName,
            PartReference = partReference,
            Quantity = request.Quantity,
            UnitPriceSnapshot = unitPrice,
            CreatedAt = DateTime.UtcNow
        };

        _context.PartsUsed.Add(partUsed);
        intervention.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Part {PartId} added to intervention {InterventionId}", request.PartId, id);
        return await GetByIdAsync(id);
    }

    public async Task<ApiResponse> RemovePartUsedAsync(Guid interventionId, Guid partUsedId)
    {
        var partUsed = await _context.PartsUsed
            .FirstOrDefaultAsync(p => p.Id == partUsedId && p.InterventionId == interventionId);

        if (partUsed == null)
        {
            return ApiResponse.FailureResponse("Part not found in intervention");
        }

        _context.PartsUsed.Remove(partUsed);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Part {PartUsedId} removed from intervention {InterventionId}", partUsedId, interventionId);
        return ApiResponse.SuccessResponse("Part removed successfully");
    }

    public async Task<ApiResponse<InterventionDto>> SetLaborAsync(Guid id, SetLaborRequest request)
    {
        var intervention = await _context.Interventions
            .Include(i => i.Labor)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (intervention == null)
        {
            return ApiResponse<InterventionDto>.FailureResponse("Intervention not found");
        }

        if (intervention.Labor != null)
        {
            intervention.Labor.Hours = request.Hours;
            intervention.Labor.HourlyRate = request.HourlyRate;
            intervention.Labor.Description = request.Description;
            intervention.Labor.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var labor = new Labor
            {
                Id = Guid.NewGuid(),
                InterventionId = id,
                Hours = request.Hours,
                HourlyRate = request.HourlyRate,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };
            _context.Labors.Add(labor);
        }

        intervention.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Labor set for intervention {InterventionId}", id);
        return await GetByIdAsync(id);
    }

    public async Task<ApiResponse> RemoveLaborAsync(Guid interventionId, Guid laborId)
    {
        var labor = await _context.Labors
            .FirstOrDefaultAsync(l => l.Id == laborId && l.InterventionId == interventionId);

        if (labor == null)
        {
            return ApiResponse.FailureResponse("Labor not found in intervention");
        }

        _context.Labors.Remove(labor);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Labor {LaborId} removed from intervention {InterventionId}", laborId, interventionId);
        return ApiResponse.SuccessResponse("Labor removed successfully");
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        var intervention = await _context.Interventions.FindAsync(id);
        if (intervention == null)
        {
            return ApiResponse.FailureResponse("Intervention not found");
        }

        intervention.IsDeleted = true;
        intervention.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Intervention {InterventionId} soft deleted", id);
        return ApiResponse.SuccessResponse("Intervention deleted successfully");
    }

    public async Task<ApiResponse> RestoreAsync(Guid id)
    {
        var intervention = await _context.Interventions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (intervention == null)
        {
            return ApiResponse.FailureResponse("Intervention not found");
        }

        intervention.IsDeleted = false;
        intervention.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Intervention {InterventionId} restored", id);
        return ApiResponse.SuccessResponse("Intervention restored successfully");
    }

    private InterventionDto MapToDto(Intervention intervention, ReclamationDto? reclamation = null)
    {
        return new InterventionDto
        {
            Id = intervention.Id,
            ReclamationId = intervention.ReclamationId,
            ReclamationTitle = reclamation?.Title ?? string.Empty,
            ClientName = reclamation?.ClientName ?? string.Empty,
            ArticleName = reclamation?.ArticleName ?? string.Empty,
            TechnicianId = intervention.TechnicianId,
            TechnicianName = intervention.Technician?.FullName,
            Status = intervention.Status,
            PlannedDate = intervention.PlannedDate,
            StartedAt = intervention.StartedAt,
            CompletedAt = intervention.CompletedAt,
            Notes = intervention.Notes,
            DiagnosticNotes = intervention.DiagnosticNotes,
            ResolutionNotes = intervention.ResolutionNotes,
            IsFree = intervention.IsFree,
            PartsUsed = intervention.PartsUsed.Select(p => new PartUsedDto
            {
                Id = p.Id,
                PartId = p.PartId,
                PartName = p.PartName,
                PartReference = p.PartReference,
                Quantity = p.Quantity,
                UnitPrice = p.UnitPriceSnapshot,
                TotalPrice = p.TotalPrice
            }).ToList(),
            Labor = intervention.Labor != null ? new List<LaborDto>
            {
                new LaborDto
                {
                    Id = intervention.Labor.Id,
                    Hours = intervention.Labor.Hours,
                    HourlyRate = intervention.Labor.HourlyRate,
                    TotalAmount = intervention.Labor.TotalAmount,
                    Description = intervention.Labor.Description
                }
            } : new List<LaborDto>(),
            HasInvoice = intervention.Invoice != null,
            InvoiceId = intervention.Invoice?.Id,
            InvoiceNumber = intervention.Invoice?.InvoiceNumber,
            PartsTotal = intervention.TotalPartsAmount,
            LaborTotal = intervention.TotalLaborAmount,
            TotalAmount = intervention.TotalAmount,
            IsDeleted = intervention.IsDeleted,
            CreatedAt = intervention.CreatedAt,
            UpdatedAt = intervention.UpdatedAt
        };
    }

    private InterventionListDto MapToListDto(Intervention intervention, Dictionary<Guid, (string Title, string ClientName)>? reclamationsDict = null)
    {
        var dto = new InterventionListDto
        {
            Id = intervention.Id,
            ReclamationId = intervention.ReclamationId,
            TechnicianId = intervention.TechnicianId,
            TechnicianName = intervention.Technician?.FullName,
            Status = intervention.Status,
            PlannedDate = intervention.PlannedDate,
            IsFree = intervention.IsFree,
            TotalAmount = intervention.TotalAmount,
            HasInvoice = intervention.Invoice != null,
            CreatedAt = intervention.CreatedAt
        };

        // Enrich with reclamation data if available
        if (reclamationsDict != null && reclamationsDict.TryGetValue(intervention.ReclamationId, out var reclamationInfo))
        {
            dto.ReclamationTitle = reclamationInfo.Title;
            dto.ClientName = reclamationInfo.ClientName;
        }

        return dto;
    }
}

