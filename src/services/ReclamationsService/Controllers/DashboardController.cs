using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Savora.ReclamationsService.Application.Services;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Dashboard;

namespace Savora.ReclamationsService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("sav")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> GetSavDashboard()
    {
        var result = await _dashboardService.GetSavDashboardAsync();
        return Ok(result);
    }

    [HttpGet("client")]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> GetClientDashboard()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _dashboardService.GetClientDashboardAsync(userId.Value);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("stats")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _dashboardService.GetSavDashboardAsync();
        return Ok(result);
    }

    [HttpGet("client-stats")]
    public async Task<IActionResult> GetClientStats()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _dashboardService.GetClientDashboardAsync(userId.Value);
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

