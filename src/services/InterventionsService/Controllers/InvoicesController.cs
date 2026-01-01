using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Savora.InterventionsService.Application.Services;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Interventions;

namespace Savora.InterventionsService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IClientApiClient _clientApiClient;

    public InvoicesController(IInvoiceService invoiceService, IClientApiClient clientApiClient)
    {
        _invoiceService = invoiceService;
        _clientApiClient = clientApiClient;
    }

    [HttpGet]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _invoiceService.GetAllAsync(pagination);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _invoiceService.GetByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpGet("intervention/{interventionId:guid}")]
    public async Task<IActionResult> GetByInterventionId(Guid interventionId)
    {
        var result = await _invoiceService.GetByInterventionIdAsync(interventionId);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpPost("intervention/{interventionId:guid}/generate")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> Generate(Guid interventionId, [FromBody] GenerateInvoiceRequest? request = null)
    {
        var result = await _invoiceService.GenerateInvoiceAsync(
            interventionId, 
            request?.ClientEmail, 
            request?.ClientName);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("order/{orderId:guid}/generate")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> GenerateFromOrder(Guid orderId, [FromBody] GenerateOrderInvoiceRequest request)
    {
        var result = await _invoiceService.GenerateInvoiceFromOrderAsync(
            orderId,
            request.TotalAmount,
            request.OrderNumber,
            request.ClientEmail,
            request.ClientName);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("my-invoices")]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> GetMyInvoices([FromQuery] PaginationParams pagination)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var client = await _clientApiClient.GetClientByUserIdAsync(userId.Value);
        if (client == null)
        {
            return NotFound(ApiResponse<PaginatedResult<InvoiceListDto>>.FailureResponse("Client not found"));
        }

        var result = await _invoiceService.GetByClientIdAsync(client.Id, pagination);
        return Ok(result);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetPdf(Guid id)
    {
        var result = await _invoiceService.GetInvoicePdfAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return File(result.Data!, "application/pdf", $"invoice-{id}.pdf");
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

public class GenerateInvoiceRequest
{
    public string? ClientEmail { get; set; }
    public string? ClientName { get; set; }
}

public class GenerateOrderInvoiceRequest
{
    public decimal TotalAmount { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    public string? ClientName { get; set; }
}

