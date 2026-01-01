using Microsoft.EntityFrameworkCore;
using Savora.InterventionsService.Domain.Entities;
using Savora.InterventionsService.Infrastructure.Data;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Interventions;
using Savora.Shared.Enums;

namespace Savora.InterventionsService.Application.Services;

public class TechnicianServiceImpl : ITechnicianService
{
    private readonly InterventionsDbContext _context;
    private readonly ILogger<TechnicianServiceImpl> _logger;

    public TechnicianServiceImpl(InterventionsDbContext context, ILogger<TechnicianServiceImpl> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<TechnicianDto>> GetByIdAsync(Guid id)
    {
        var technician = await _context.Technicians
            .Include(t => t.Interventions)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (technician == null)
        {
            return ApiResponse<TechnicianDto>.FailureResponse("Technician not found");
        }

        return ApiResponse<TechnicianDto>.SuccessResponse(MapToDto(technician));
    }

    public async Task<ApiResponse<PaginatedResult<TechnicianDto>>> GetAllAsync(PaginationParams pagination, bool? availableOnly = null)
    {
        var query = _context.Technicians
            .Include(t => t.Interventions)
            .AsQueryable();

        if (availableOnly == true)
        {
            query = query.Where(t => t.IsAvailable);
        }

        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            var searchLower = pagination.SearchTerm.ToLower();
            query = query.Where(t =>
                t.FullName.ToLower().Contains(searchLower) ||
                t.Email.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync();

        query = pagination.SortBy?.ToLower() switch
        {
            "name" => pagination.SortDescending ? query.OrderByDescending(t => t.FullName) : query.OrderBy(t => t.FullName),
            "email" => pagination.SortDescending ? query.OrderByDescending(t => t.Email) : query.OrderBy(t => t.Email),
            _ => query.OrderBy(t => t.FullName)
        };

        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var dtos = items.Select(MapToDto).ToList();
        var result = new PaginatedResult<TechnicianDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize);
        return ApiResponse<PaginatedResult<TechnicianDto>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<List<TechnicianDto>>> GetAvailableAsync()
    {
        var technicians = await _context.Technicians
            .Include(t => t.Interventions)
            .Where(t => t.IsAvailable)
            .OrderBy(t => t.FullName)
            .ToListAsync();

        var dtos = technicians.Select(MapToDto).ToList();
        return ApiResponse<List<TechnicianDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<TechnicianDto>> CreateAsync(CreateTechnicianRequest request)
    {
        var existingEmail = await _context.Technicians.AnyAsync(t => t.Email == request.Email);
        if (existingEmail)
        {
            return ApiResponse<TechnicianDto>.FailureResponse("Email already in use");
        }

        var technician = new Technician
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Skills = request.Skills,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Technicians.Add(technician);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Technician {TechnicianId} created", technician.Id);
        return await GetByIdAsync(technician.Id);
    }

    public async Task<ApiResponse<TechnicianDto>> UpdateAsync(Guid id, UpdateTechnicianRequest request)
    {
        var technician = await _context.Technicians.FindAsync(id);
        if (technician == null)
        {
            return ApiResponse<TechnicianDto>.FailureResponse("Technician not found");
        }

        technician.FullName = request.FullName;
        technician.Phone = request.Phone;
        technician.Skills = request.Skills;
        technician.IsAvailable = request.IsAvailable;
        technician.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Technician {TechnicianId} updated", id);
        return await GetByIdAsync(id);
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        var technician = await _context.Technicians.FindAsync(id);
        if (technician == null)
        {
            return ApiResponse.FailureResponse("Technician not found");
        }

        technician.IsDeleted = true;
        technician.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Technician {TechnicianId} soft deleted", id);
        return ApiResponse.SuccessResponse("Technician deleted successfully");
    }

    public async Task<ApiResponse> SetAvailabilityAsync(Guid id, bool isAvailable)
    {
        var technician = await _context.Technicians.FindAsync(id);
        if (technician == null)
        {
            return ApiResponse.FailureResponse("Technician not found");
        }

        technician.IsAvailable = isAvailable;
        technician.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Technician {TechnicianId} availability set to {IsAvailable}", id, isAvailable);
        return ApiResponse.SuccessResponse($"Technician availability set to {(isAvailable ? "available" : "unavailable")}");
    }

    private TechnicianDto MapToDto(Technician technician)
    {
        return new TechnicianDto
        {
            Id = technician.Id,
            UserId = technician.UserId ?? Guid.Empty,
            FullName = technician.FullName,
            Email = technician.Email,
            Phone = technician.Phone,
            Skills = technician.Skills,
            IsAvailable = technician.IsAvailable,
            ActiveInterventions = technician.Interventions.Count(i => 
                i.Status == InterventionStatus.Planned || i.Status == InterventionStatus.InProgress),
            CompletedInterventions = technician.Interventions.Count(i => i.Status == InterventionStatus.Completed),
            IsDeleted = technician.IsDeleted,
            CreatedAt = technician.CreatedAt
        };
    }
}

