using Microsoft.EntityFrameworkCore;
using Savora.ArticlesService.Domain.Entities;
using Savora.ArticlesService.Infrastructure.Data;
using Savora.Shared.DTOs.Articles;
using Savora.Shared.DTOs.Common;

namespace Savora.ArticlesService.Application.Services;

public class PartService : IPartService
{
    private readonly ArticlesDbContext _context;
    private readonly ILogger<PartService> _logger;

    public PartService(ArticlesDbContext context, ILogger<PartService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<PartDto>> GetByIdAsync(Guid id)
    {
        var part = await _context.Parts.FindAsync(id);
        if (part == null)
        {
            return ApiResponse<PartDto>.FailureResponse("Part not found");
        }

        return ApiResponse<PartDto>.SuccessResponse(MapToDto(part));
    }

    public async Task<ApiResponse<PaginatedResult<PartDto>>> GetAllAsync(PaginationParams pagination, PartFilterParams? filter = null)
    {
        var query = _context.Parts.AsQueryable();

        // Apply filters
        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Category))
                query = query.Where(p => p.Category != null && p.Category.ToLower().Contains(filter.Category.ToLower()));

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.UnitPrice >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.UnitPrice <= filter.MaxPrice.Value);

            if (filter.LowStockOnly == true)
                query = query.Where(p => p.StockQuantity <= p.MinStockLevel);
        }

        // Apply search
        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            var searchLower = pagination.SearchTerm.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchLower) ||
                p.Reference.ToLower().Contains(searchLower) ||
                (p.Description != null && p.Description.ToLower().Contains(searchLower)));
        }

        var totalCount = await query.CountAsync();

        // Apply sorting
        query = pagination.SortBy?.ToLower() switch
        {
            "name" => pagination.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "price" => pagination.SortDescending ? query.OrderByDescending(p => p.UnitPrice) : query.OrderBy(p => p.UnitPrice),
            "stock" => pagination.SortDescending ? query.OrderByDescending(p => p.StockQuantity) : query.OrderBy(p => p.StockQuantity),
            _ => query.OrderBy(p => p.Name)
        };

        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var dtos = items.Select(MapToDto).ToList();
        var result = new PaginatedResult<PartDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize);
        return ApiResponse<PaginatedResult<PartDto>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<List<PartDto>>> GetLowStockPartsAsync()
    {
        var parts = await _context.Parts
            .Where(p => p.StockQuantity <= p.MinStockLevel)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync();

        var dtos = parts.Select(MapToDto).ToList();
        return ApiResponse<List<PartDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<PartDto>> CreateAsync(CreatePartRequest request)
    {
        var existingRef = await _context.Parts.AnyAsync(p => p.Reference == request.Reference);
        if (existingRef)
        {
            return ApiResponse<PartDto>.FailureResponse("Part reference already exists");
        }

        var part = new Part
        {
            Id = Guid.NewGuid(),
            Reference = request.Reference,
            Name = request.Name,
            Description = request.Description,
            UnitPrice = request.UnitPrice,
            StockQuantity = request.StockQuantity,
            MinStockLevel = request.MinStockLevel,
            Category = request.Category,
            CreatedAt = DateTime.UtcNow
        };

        _context.Parts.Add(part);

        // Record initial stock movement
        if (request.StockQuantity > 0)
        {
            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                PartId = part.Id,
                QuantityChange = request.StockQuantity,
                PreviousQuantity = 0,
                NewQuantity = request.StockQuantity,
                MovementType = "IN",
                Reason = "Initial stock",
                CreatedAt = DateTime.UtcNow
            };
            _context.StockMovements.Add(movement);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Part {PartId} created with initial stock {Stock}", part.Id, request.StockQuantity);
        return ApiResponse<PartDto>.SuccessResponse(MapToDto(part), "Part created successfully");
    }

    public async Task<ApiResponse<PartDto>> UpdateAsync(Guid id, UpdatePartRequest request)
    {
        var part = await _context.Parts.FindAsync(id);
        if (part == null)
        {
            return ApiResponse<PartDto>.FailureResponse("Part not found");
        }

        var existingRef = await _context.Parts.AnyAsync(p => p.Reference == request.Reference && p.Id != id);
        if (existingRef)
        {
            return ApiResponse<PartDto>.FailureResponse("Part reference already exists");
        }

        var stockDiff = request.StockQuantity - part.StockQuantity;

        part.Reference = request.Reference;
        part.Name = request.Name;
        part.Description = request.Description;
        part.UnitPrice = request.UnitPrice;
        part.MinStockLevel = request.MinStockLevel;
        part.Category = request.Category;
        part.UpdatedAt = DateTime.UtcNow;

        // Record stock adjustment if quantity changed
        if (stockDiff != 0)
        {
            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                PartId = part.Id,
                QuantityChange = stockDiff,
                PreviousQuantity = part.StockQuantity,
                NewQuantity = request.StockQuantity,
                MovementType = "ADJUSTMENT",
                Reason = "Manual stock update",
                CreatedAt = DateTime.UtcNow
            };
            _context.StockMovements.Add(movement);
            part.StockQuantity = request.StockQuantity;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Part {PartId} updated", part.Id);
        return ApiResponse<PartDto>.SuccessResponse(MapToDto(part), "Part updated successfully");
    }

    public async Task<ApiResponse> DeleteAsync(Guid id, string deletedBy)
    {
        var part = await _context.Parts.FindAsync(id);
        if (part == null)
        {
            return ApiResponse.FailureResponse("Part not found");
        }

        part.IsDeleted = true;
        part.DeletedBy = deletedBy;
        part.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Part {PartId} soft deleted by {DeletedBy}", id, deletedBy);
        return ApiResponse.SuccessResponse("Part deleted successfully");
    }

    public async Task<ApiResponse> RestoreAsync(Guid id)
    {
        var part = await _context.Parts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (part == null)
        {
            return ApiResponse.FailureResponse("Part not found");
        }

        part.IsDeleted = false;
        part.DeletedBy = null;
        part.DeletedAt = null;
        part.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Part {PartId} restored", id);
        return ApiResponse.SuccessResponse("Part restored successfully");
    }

    public async Task<ApiResponse> UpdateStockAsync(Guid id, int quantityChange, string reason, Guid? relatedEntityId = null, string? relatedEntityType = null, string? changedBy = null)
    {
        var part = await _context.Parts.FindAsync(id);
        if (part == null)
        {
            return ApiResponse.FailureResponse("Part not found");
        }

        var previousQuantity = part.StockQuantity;
        var newQuantity = previousQuantity + quantityChange;

        if (newQuantity < 0)
        {
            return ApiResponse.FailureResponse("Insufficient stock");
        }

        part.StockQuantity = newQuantity;
        part.UpdatedAt = DateTime.UtcNow;

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            PartId = part.Id,
            QuantityChange = quantityChange,
            PreviousQuantity = previousQuantity,
            NewQuantity = newQuantity,
            MovementType = quantityChange > 0 ? "IN" : "OUT",
            Reason = reason,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            CreatedBy = changedBy,
            CreatedAt = DateTime.UtcNow
        };
        _context.StockMovements.Add(movement);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Part {PartId} stock updated: {Previous} -> {New}", id, previousQuantity, newQuantity);
        return ApiResponse.SuccessResponse("Stock updated successfully");
    }

    public async Task<ApiResponse<PartDto>> DeductStockAsync(Guid partId, int quantity, Guid interventionId, string? changedBy)
    {
        var part = await _context.Parts.FindAsync(partId);
        if (part == null)
        {
            return ApiResponse<PartDto>.FailureResponse("Part not found");
        }

        if (part.StockQuantity < quantity)
        {
            return ApiResponse<PartDto>.FailureResponse($"Insufficient stock. Available: {part.StockQuantity}");
        }

        var previousQuantity = part.StockQuantity;
        part.StockQuantity -= quantity;
        part.UpdatedAt = DateTime.UtcNow;

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            PartId = partId,
            QuantityChange = -quantity,
            PreviousQuantity = previousQuantity,
            NewQuantity = part.StockQuantity,
            MovementType = "OUT",
            Reason = "Used in intervention",
            RelatedEntityId = interventionId,
            RelatedEntityType = "Intervention",
            CreatedBy = changedBy,
            CreatedAt = DateTime.UtcNow
        };
        _context.StockMovements.Add(movement);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deducted {Quantity} units from part {PartId} for intervention {InterventionId}", quantity, partId, interventionId);
        return ApiResponse<PartDto>.SuccessResponse(MapToDto(part), "Stock deducted successfully");
    }

    private static PartDto MapToDto(Part part)
    {
        return new PartDto
        {
            Id = part.Id,
            Reference = part.Reference,
            Name = part.Name,
            Description = part.Description,
            UnitPrice = part.UnitPrice,
            StockQuantity = part.StockQuantity,
            MinStockLevel = part.MinStockLevel,
            Category = part.Category,
            IsLowStock = part.IsLowStock,
            IsDeleted = part.IsDeleted,
            CreatedAt = part.CreatedAt,
            UpdatedAt = part.UpdatedAt
        };
    }
}

