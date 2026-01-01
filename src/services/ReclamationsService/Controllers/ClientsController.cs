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
public class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _clientService.GetAllAsync(pagination);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _clientService.GetByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentClient()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var result = await _clientService.GetByUserIdAsync(userId.Value);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpGet("user/{userId:guid}")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> GetByUserId(Guid userId)
    {
        var result = await _clientService.GetByUserIdAsync(userId);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest request)
    {
        // For client registration, userId comes from the token
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        request.UserId = userId.Value;
        var result = await _clientService.CreateAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentClient([FromBody] UpdateClientRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var clientResult = await _clientService.GetByUserIdAsync(userId.Value);
        if (!clientResult.Success)
        {
            return NotFound(clientResult);
        }

        var result = await _clientService.UpdateAsync(clientResult.Data!.Id, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientRequest request)
    {
        var result = await _clientService.UpdateAsync(id, request);
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
        var result = await _clientService.DeleteAsync(id);
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

