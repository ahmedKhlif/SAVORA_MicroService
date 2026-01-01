using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Savora.ReclamationsService.Application.Services;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;

namespace Savora.ReclamationsService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReclamationsController : ControllerBase
{
    private readonly IReclamationService _reclamationService;
    private readonly IClientService _clientService;

    public ReclamationsController(IReclamationService reclamationService, IClientService clientService)
    {
        _reclamationService = reclamationService;
        _clientService = clientService;
    }

    [HttpGet]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination, [FromQuery] ReclamationFilterParams? filter = null)
    {
        var result = await _reclamationService.GetAllAsync(pagination, filter);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _reclamationService.GetByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }

        // Verify client can only see their own reclamations
        if (!User.IsInRole("ResponsableSAV"))
        {
            var userId = GetUserId();
            var clientResult = await _clientService.GetByUserIdAsync(userId!.Value);
            if (!clientResult.Success || clientResult.Data!.Id != result.Data!.ClientId)
            {
                return Forbid();
            }
        }

        return Ok(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyReclamations()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var clientResult = await _clientService.GetByUserIdAsync(userId.Value);
        if (!clientResult.Success)
        {
            return NotFound(clientResult);
        }

        var result = await _reclamationService.GetByClientIdAsync(clientResult.Data!.Id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReclamationRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var clientResult = await _clientService.GetByUserIdAsync(userId.Value);
        if (!clientResult.Success)
        {
            return BadRequest("Client profile not found. Please complete your profile first.");
        }

        var createdBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
        var result = await _reclamationService.CreateAsync(request, clientResult.Data!.Id, createdBy);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReclamationRequest request)
    {
        var updatedBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
        var result = await _reclamationService.UpdateAsync(id, request, updatedBy);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateReclamationStatusRequest request)
    {
        var changedBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
        var changedByUserId = GetUserId(); // Get the UserId of the person changing the status
        var result = await _reclamationService.UpdateStatusAsync(id, request, changedBy, changedByUserId);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPut("{id:guid}/priority")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> UpdatePriority(Guid id, [FromBody] UpdateReclamationPriorityRequest request)
    {
        var changedBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
        var result = await _reclamationService.UpdatePriorityAsync(id, request, changedBy);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseReclamationRequest? request = null)
    {
        var closedBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
        var result = await _reclamationService.CloseAsync(id, closedBy, request?.Comment);
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
        var deletedBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
        var result = await _reclamationService.DeleteAsync(id, deletedBy);
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
        var result = await _reclamationService.RestoreAsync(id);
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

public class CloseReclamationRequest
{
    public string? Comment { get; set; }
}

