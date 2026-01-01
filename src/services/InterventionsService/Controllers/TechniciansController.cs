using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Savora.InterventionsService.Application.Services;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Interventions;

namespace Savora.InterventionsService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ResponsableSAV")]
public class TechniciansController : ControllerBase
{
    private readonly ITechnicianService _technicianService;

    public TechniciansController(ITechnicianService technicianService)
    {
        _technicianService = technicianService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination, [FromQuery] bool? availableOnly = null)
    {
        var result = await _technicianService.GetAllAsync(pagination, availableOnly);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _technicianService.GetByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable()
    {
        var result = await _technicianService.GetAvailableAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTechnicianRequest request)
    {
        var result = await _technicianService.CreateAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTechnicianRequest request)
    {
        var result = await _technicianService.UpdateAsync(id, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _technicianService.DeleteAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpPut("{id:guid}/availability")]
    public async Task<IActionResult> SetAvailability(Guid id, [FromBody] SetAvailabilityRequest request)
    {
        var result = await _technicianService.SetAvailabilityAsync(id, request.IsAvailable);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}

public class SetAvailabilityRequest
{
    public bool IsAvailable { get; set; }
}

