using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Savora.ArticlesService.Application.Services;
using Savora.Shared.DTOs.Articles;
using Savora.Shared.DTOs.Common;

namespace Savora.ArticlesService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PartsController : ControllerBase
{
    private readonly IPartService _partService;

    public PartsController(IPartService partService)
    {
        _partService = partService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination, [FromQuery] PartFilterParams? filter = null)
    {
        var result = await _partService.GetAllAsync(pagination, filter);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _partService.GetByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpGet("low-stock")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> GetLowStock()
    {
        var result = await _partService.GetLowStockPartsAsync();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> Create([FromBody] CreatePartRequest request)
    {
        var result = await _partService.CreateAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePartRequest request)
    {
        var result = await _partService.UpdateAsync(id, request);
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
        var result = await _partService.DeleteAsync(id, deletedBy);
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
        var result = await _partService.RestoreAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpPost("{id:guid}/stock")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> UpdateStock(Guid id, [FromBody] UpdateStockRequest request)
    {
        var changedBy = User.FindFirst(ClaimTypes.Email)?.Value;
        var result = await _partService.UpdateStockAsync(id, request.QuantityChange, request.Reason ?? "Manual adjustment", changedBy: changedBy);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("{id:guid}/deduct")]
    public async Task<IActionResult> DeductStock(Guid id, [FromBody] DeductStockRequest request)
    {
        var changedBy = User.FindFirst(ClaimTypes.Email)?.Value;
        var result = await _partService.DeductStockAsync(id, request.Quantity, request.InterventionId, changedBy);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}

public class DeductStockRequest
{
    public int Quantity { get; set; }
    public Guid InterventionId { get; set; }
}

