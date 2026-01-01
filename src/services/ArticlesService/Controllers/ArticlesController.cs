using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Savora.ArticlesService.Application.Services;
using Savora.ArticlesService.Domain.Entities;
using Savora.ArticlesService.Infrastructure.Data;
using Savora.Shared.DTOs.Articles;
using Savora.Shared.DTOs.Common;

namespace Savora.ArticlesService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ArticlesController : ControllerBase
{
    private readonly ArticlesDbContext _context;
    private readonly ILogger<ArticlesController> _logger;
    private readonly IClientApiClient _clientApiClient;

    public ArticlesController(ArticlesDbContext context, ILogger<ArticlesController> logger, IClientApiClient clientApiClient)
    {
        _context = context;
        _logger = logger;
        _clientApiClient = clientApiClient;
    }

    [HttpGet]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] string? category = null)
    {
        // Only return client articles (SAV application - no catalog)
        var query = _context.Articles.Where(a => a.ClientId != null && a.ClientId != Guid.Empty);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(a => 
                a.Reference.Contains(search) || 
                a.Name.Contains(search) || 
                a.Brand.Contains(search) ||
                a.SerialNumber.Contains(search));
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(a => a.Category == category);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Enrich with client names
        var clientIds = items.Where(a => a.ClientId.HasValue).Select(a => a.ClientId!.Value).Distinct().ToList();
        var clientsDict = new Dictionary<Guid, string>();
        
        foreach (var clientId in clientIds)
        {
            try
            {
                var client = await _clientApiClient.GetClientByIdAsync(clientId);
                if (client != null)
                {
                    clientsDict[clientId] = client.FullName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch client {ClientId} for article list", clientId);
            }
        }

        var dtos = items.Select(a => new ArticleDto
        {
            Id = a.Id,
            Reference = a.Reference,
            Name = a.Name,
            Brand = a.Brand,
            Category = a.Category,
            Price = a.Price,
            SerialNumber = a.SerialNumber,
            PurchaseDate = a.PurchaseDate,
            WarrantyMonths = a.WarrantyMonths,
            ClientId = a.ClientId ?? Guid.Empty,
            ClientName = a.ClientId.HasValue && clientsDict.TryGetValue(a.ClientId.Value, out var clientName) ? clientName : null,
            IsUnderWarranty = a.IsUnderWarranty,
            WarrantyEndDate = a.WarrantyEndDate ?? DateTime.UtcNow,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt,
            IsDeleted = a.IsDeleted
        }).ToList();

        var result = new PaginatedResult<ArticleDto>(dtos, totalCount, page, pageSize);
        return Ok(ApiResponse<PaginatedResult<ArticleDto>>.SuccessResponse(result));
    }


    [HttpGet("my-articles")]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> GetMyArticles()
    {
        // Get the current user's client ID
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ApiResponse<List<ArticleDto>>.FailureResponse("User ID not found"));
        }

        // Get client ID from ReclamationsService
        var client = await _clientApiClient.GetClientByUserIdAsync(userId);
        if (client == null)
        {
            return Ok(ApiResponse<List<ArticleDto>>.SuccessResponse(new List<ArticleDto>()));
        }

        var articles = await _context.Articles
            .Where(a => a.ClientId == client.Id && !a.IsDeleted)
            .ToListAsync();

        var dtos = articles.Select(a => new ArticleDto
        {
            Id = a.Id,
            Reference = a.Reference,
            Name = a.Name,
            Brand = a.Brand,
            Category = a.Category,
            Price = a.Price,
            SerialNumber = a.SerialNumber,
            PurchaseDate = a.PurchaseDate,
            WarrantyMonths = a.WarrantyMonths,
            ClientId = a.ClientId ?? Guid.Empty,
            ClientName = client.FullName, // Use the client we already fetched
            IsUnderWarranty = a.IsUnderWarranty,
            WarrantyEndDate = a.WarrantyEndDate ?? DateTime.UtcNow,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt,
            IsDeleted = a.IsDeleted
        }).ToList();

        return Ok(ApiResponse<List<ArticleDto>>.SuccessResponse(dtos));
    }

    [HttpGet("client/{clientId}")]
    [Authorize(Roles = "ResponsableSAV,Client")]
    public async Task<IActionResult> GetByClientId(Guid clientId)
    {
        // If user is a Client, they can only see their own articles
        if (User.IsInRole("Client"))
        {
            // Get the current user's client ID
            var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(ApiResponse<List<ArticleDto>>.FailureResponse("User ID not found"));
            }

            // Note: In production, you'd verify that clientId belongs to the current user
            // by calling ReclamationsService to get the client ID for this user
            // For now, we'll allow the request but log a warning
            _logger.LogWarning("Client {UserId} requesting articles for client {ClientId} - verification needed", userId, clientId);
        }

        // Get client name
        string? clientName = null;
        try
        {
            var client = await _clientApiClient.GetClientByIdAsync(clientId);
            clientName = client?.FullName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch client {ClientId} for articles", clientId);
        }

        var articles = await _context.Articles
            .Where(a => a.ClientId == clientId && !a.IsDeleted)
            .ToListAsync();

        var dtos = articles.Select(a => new ArticleDto
        {
            Id = a.Id,
            Reference = a.Reference,
            Name = a.Name,
            Brand = a.Brand,
            Category = a.Category,
            Price = a.Price,
            SerialNumber = a.SerialNumber,
            PurchaseDate = a.PurchaseDate,
            WarrantyMonths = a.WarrantyMonths,
            ClientId = a.ClientId ?? Guid.Empty,
            ClientName = clientName,
            IsUnderWarranty = a.IsUnderWarranty,
            WarrantyEndDate = a.WarrantyEndDate ?? DateTime.UtcNow,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt,
            IsDeleted = a.IsDeleted
        }).ToList();

        return Ok(ApiResponse<List<ArticleDto>>.SuccessResponse(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article == null)
        {
            return NotFound(ApiResponse<ArticleDto>.FailureResponse("Article not found"));
        }

        // Get client name if ClientId exists
        string? clientName = null;
        if (article.ClientId.HasValue)
        {
            try
            {
                var client = await _clientApiClient.GetClientByIdAsync(article.ClientId.Value);
                clientName = client?.FullName;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch client {ClientId} for article {ArticleId}", article.ClientId, id);
            }
        }

        var dto = new ArticleDto
        {
            Id = article.Id,
            Reference = article.Reference,
            Name = article.Name,
            Brand = article.Brand,
            Category = article.Category,
            Price = article.Price,
            SerialNumber = article.SerialNumber,
            PurchaseDate = article.PurchaseDate,
            WarrantyMonths = article.WarrantyMonths,
            ClientId = article.ClientId ?? Guid.Empty,
            ClientName = clientName,
            IsUnderWarranty = article.IsUnderWarranty,
            WarrantyEndDate = article.WarrantyEndDate ?? DateTime.UtcNow,
            CreatedAt = article.CreatedAt,
            UpdatedAt = article.UpdatedAt,
            IsDeleted = article.IsDeleted
        };

        return Ok(ApiResponse<ArticleDto>.SuccessResponse(dto));
    }

    [HttpPost]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> Create([FromBody] CreateArticleRequest request)
    {
        // Validate ClientId is required for SAV application
        if (request.ClientId == Guid.Empty)
        {
            return BadRequest(ApiResponse<ArticleDto>.FailureResponse("Client ID is required. Articles must be associated with a client."));
        }

        // Verify client exists
        var client = await _clientApiClient.GetClientByIdAsync(request.ClientId);
        if (client == null)
        {
            return BadRequest(ApiResponse<ArticleDto>.FailureResponse("Client not found"));
        }

        var article = new Article
        {
            Id = Guid.NewGuid(),
            Reference = request.Reference,
            Name = request.Name,
            Brand = request.Brand,
            Category = request.Category,
            Price = request.Price,
            SerialNumber = request.SerialNumber,
            PurchaseDate = request.PurchaseDate,
            WarrantyMonths = request.WarrantyMonths,
            ClientId = request.ClientId, // Required - always set for SAV
            CreatedAt = DateTime.UtcNow
        };

        _context.Articles.Add(article);
        await _context.SaveChangesAsync();

        var dto = new ArticleDto
        {
            Id = article.Id,
            Reference = article.Reference,
            Name = article.Name,
            Brand = article.Brand,
            Category = article.Category,
            Price = article.Price,
            SerialNumber = article.SerialNumber,
            PurchaseDate = article.PurchaseDate,
            WarrantyMonths = article.WarrantyMonths,
            ClientId = article.ClientId ?? Guid.Empty,
            IsUnderWarranty = article.IsUnderWarranty,
            WarrantyEndDate = article.WarrantyEndDate ?? DateTime.UtcNow,
            CreatedAt = article.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = article.Id }, ApiResponse<ArticleDto>.SuccessResponse(dto));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateArticleRequest request)
    {
        // Validate ClientId is required for SAV application
        if (request.ClientId == Guid.Empty)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse("Client ID is required. Articles must be associated with a client."));
        }

        // Verify client exists
        var client = await _clientApiClient.GetClientByIdAsync(request.ClientId);
        if (client == null)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse("Client not found"));
        }

        var article = await _context.Articles.FindAsync(id);
        if (article == null)
        {
            return NotFound(ApiResponse<bool>.FailureResponse("Article not found"));
        }

        article.Reference = request.Reference;
        article.Name = request.Name;
        article.Brand = request.Brand;
        article.Category = request.Category;
        article.SerialNumber = request.SerialNumber;
        article.PurchaseDate = request.PurchaseDate;
        article.WarrantyMonths = request.WarrantyMonths;
        article.Price = request.Price;
        article.ClientId = request.ClientId; // Update client association
        article.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ResponsableSAV")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article == null)
        {
            return NotFound(ApiResponse<bool>.FailureResponse("Article not found"));
        }

        article.IsDeleted = true;
        article.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }
}

