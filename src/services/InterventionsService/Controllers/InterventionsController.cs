using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Savora.InterventionsService.Application.Services;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Interventions;
using Savora.Shared.Enums;

namespace Savora.InterventionsService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InterventionsController : ControllerBase
{
    private readonly IInterventionService _interventionService;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IReclamationApiClient _reclamationApiClient;
    private readonly IArticleApiClient _articleApiClient;
    private readonly ILogger<InterventionsController> _logger;

    public InterventionsController(
        IInterventionService interventionService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IReclamationApiClient reclamationApiClient,
        IArticleApiClient articleApiClient,
        ILogger<InterventionsController> logger)
    {
        _interventionService = interventionService;
        _httpClient = httpClientFactory.CreateClient();
        _configuration = configuration;
        _reclamationApiClient = reclamationApiClient;
        _articleApiClient = articleApiClient;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination, [FromQuery] InterventionFilterParams? filter = null)
    {
        var result = await _interventionService.GetAllAsync(pagination, filter);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _interventionService.GetByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }

        // Verify client can only see interventions for their own reclamations
        // The reclamation service already verifies client ownership when fetching
        // If the call succeeds, the client has access to the reclamation
        if (!User.IsInRole("ResponsableSAV") && result.Data != null)
        {
            var reclamation = await _reclamationApiClient.GetReclamationAsync(result.Data.ReclamationId);
            if (reclamation == null)
            {
                // If reclamation service returns null, it means either:
                // 1. Reclamation doesn't exist
                // 2. Client doesn't have access (Forbid response)
                return Forbid();
            }
        }

        return Ok(result);
    }

    [HttpGet("reclamation/{reclamationId:guid}")]
    public async Task<IActionResult> GetByReclamationId(Guid reclamationId)
    {
        // Verify client can only see interventions for their own reclamations
        // The reclamation service already verifies client ownership when fetching
        if (!User.IsInRole("ResponsableSAV"))
        {
            var reclamation = await _reclamationApiClient.GetReclamationAsync(reclamationId);
            if (reclamation == null)
            {
                // If reclamation service returns null, it means either:
                // 1. Reclamation doesn't exist
                // 2. Client doesn't have access (Forbid response)
                return Forbid();
            }
        }

        var result = await _interventionService.GetByReclamationIdAsync(reclamationId);
        return Ok(result);
    }

    [HttpGet("technician/{technicianId:guid}")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> GetByTechnicianId(Guid technicianId)
    {
        var result = await _interventionService.GetByTechnicianIdAsync(technicianId);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> Create([FromBody] CreateInterventionWithWarrantyRequest request)
    {
        // Check if article is under warranty (would call Articles service in production)
        var isFree = request.IsUnderWarranty;

        var createRequest = new CreateInterventionRequest
        {
            ReclamationId = request.ReclamationId,
            TechnicianId = request.TechnicianId,
            PlannedDate = request.PlannedDate,
            Notes = request.Notes
        };

        var result = await _interventionService.CreateAsync(createRequest, isFree);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInterventionRequest request)
    {
        var result = await _interventionService.UpdateAsync(id, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPut("{id:guid}/reschedule")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> Reschedule(Guid id, [FromBody] RescheduleRequest request)
    {
        var updateRequest = new UpdateInterventionRequest
        {
            PlannedDate = request.PlannedDate
        };
        
        var result = await _interventionService.UpdateAsync(id, updateRequest);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Intervention reprogrammée" });
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateInterventionStatusRequest request)
    {
        var result = await _interventionService.UpdateStatusAsync(id, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPut("{id:guid}/assign/{technicianId:guid}")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> AssignTechnician(Guid id, Guid technicianId)
    {
        var result = await _interventionService.AssignTechnicianAsync(id, technicianId);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        
        // Return updated intervention
        var interventionResult = await _interventionService.GetByIdAsync(id);
        if (!interventionResult.Success)
        {
            return BadRequest(interventionResult);
        }
        return Ok(interventionResult);
    }

    [HttpPost("{id:guid}/parts")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> AddPart(Guid id, [FromBody] AddPartRequest request)
    {
        // Fetch part details from ArticlesService
        var part = await _articleApiClient.GetPartAsync(request.PartId);
        if (part == null)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse("Part not found"));
        }

        // Check if stock is sufficient
        if (part.StockQuantity < request.Quantity)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse($"Stock insuffisant. Disponible: {part.StockQuantity}, Demandé: {request.Quantity}"));
        }

        // Deduct stock from ArticlesService
        var stockDeducted = await _articleApiClient.DeductStockAsync(request.PartId, request.Quantity, id);
        if (!stockDeducted)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse("Erreur lors de la déduction du stock"));
        }

        var addRequest = new AddPartUsedRequest
        {
            PartId = request.PartId,
            Quantity = request.Quantity
        };

        var result = await _interventionService.AddPartUsedAsync(
            id, 
            addRequest, 
            part.Name, 
            part.Reference, 
            part.UnitPrice);

        if (!result.Success)
        {
            // If adding part failed, restore the stock
            await _articleApiClient.RestoreStockAsync(request.PartId, request.Quantity, id);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("{id:guid}/parts/{partUsedId:guid}")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> RemovePart(Guid id, Guid partUsedId)
    {
        // Get intervention to find the part used details before removing
        var interventionResult = await _interventionService.GetByIdAsync(id);
        if (!interventionResult.Success || interventionResult.Data == null)
        {
            return BadRequest(interventionResult);
        }

        // Find the part used to get PartId and Quantity
        var partUsed = interventionResult.Data.PartsUsed?.FirstOrDefault(p => p.Id == partUsedId);
        if (partUsed == null)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse("Part not found in intervention"));
        }

        // Remove the part from intervention
        var removeResult = await _interventionService.RemovePartUsedAsync(id, partUsedId);
        if (!removeResult.Success)
        {
            return BadRequest(removeResult);
        }

        // Restore stock in ArticlesService
        var stockRestored = await _articleApiClient.RestoreStockAsync(partUsed.PartId, partUsed.Quantity, id);
        if (!stockRestored)
        {
            _logger.LogWarning("Failed to restore stock for part {PartId} after removing from intervention {InterventionId}", partUsed.PartId, id);
            // Continue anyway as the part is already removed
        }
        
        // Return updated intervention
        var updatedInterventionResult = await _interventionService.GetByIdAsync(id);
        if (!updatedInterventionResult.Success)
        {
            return BadRequest(updatedInterventionResult);
        }
        return Ok(updatedInterventionResult);
    }

    [HttpDelete("{id:guid}/labor/{laborId:guid}")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> RemoveLabor(Guid id, Guid laborId)
    {
        var removeResult = await _interventionService.RemoveLaborAsync(id, laborId);
        if (!removeResult.Success)
        {
            return BadRequest(removeResult);
        }
        
        // Return updated intervention
        var interventionResult = await _interventionService.GetByIdAsync(id);
        if (!interventionResult.Success)
        {
            return BadRequest(interventionResult);
        }
        return Ok(interventionResult);
    }

    [HttpPut("{id:guid}/labor")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> SetLabor(Guid id, [FromBody] SetLaborRequest request)
    {
        var result = await _interventionService.SetLaborAsync(id, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("{id:guid}/labor")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> AddLabor(Guid id, [FromBody] AddLaborRequest request)
    {
        // Convert AddLaborRequest to SetLaborRequest
        var setLaborRequest = new SetLaborRequest
        {
            Hours = request.Hours,
            HourlyRate = request.HourlyRate,
            Description = request.Description
        };
        
        var result = await _interventionService.SetLaborAsync(id, setLaborRequest);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> StartIntervention(Guid id)
    {
        var result = await _interventionService.UpdateStatusAsync(id, new UpdateInterventionStatusRequest
        {
            NewStatus = InterventionStatus.InProgress
        });
        
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> CompleteIntervention(Guid id)
    {
        var result = await _interventionService.UpdateStatusAsync(id, new UpdateInterventionStatusRequest
        {
            NewStatus = InterventionStatus.Completed
        });
        
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> CancelIntervention(Guid id)
    {
        var result = await _interventionService.UpdateStatusAsync(id, new UpdateInterventionStatusRequest
        {
            NewStatus = InterventionStatus.Cancelled
        });
        
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _interventionService.DeleteAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var result = await _interventionService.RestoreAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}

public class CreateInterventionWithWarrantyRequest
{
    public Guid ReclamationId { get; set; }
    public Guid? TechnicianId { get; set; }
    public DateTime PlannedDate { get; set; }
    public string? Notes { get; set; }
    public bool IsUnderWarranty { get; set; }
}

public class AddPartWithDetailsRequest
{
    public Guid PartId { get; set; }
    public int Quantity { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartReference { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}

public class RescheduleRequest
{
    public DateTime PlannedDate { get; set; }
}

