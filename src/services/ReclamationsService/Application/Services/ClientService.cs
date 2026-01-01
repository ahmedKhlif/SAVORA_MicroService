using Microsoft.EntityFrameworkCore;
using Savora.ReclamationsService.Domain.Entities;
using Savora.ReclamationsService.Infrastructure.Data;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;

namespace Savora.ReclamationsService.Application.Services;

public class ClientServiceImpl : IClientService
{
    private readonly ReclamationsDbContext _context;
    private readonly ILogger<ClientServiceImpl> _logger;

    public ClientServiceImpl(ReclamationsDbContext context, ILogger<ClientServiceImpl> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<ClientDto>> GetByIdAsync(Guid id)
    {
        var client = await _context.Clients
            .Include(c => c.Reclamations)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null)
        {
            return ApiResponse<ClientDto>.FailureResponse("Client not found");
        }

        return ApiResponse<ClientDto>.SuccessResponse(MapToDto(client));
    }

    public async Task<ApiResponse<ClientDto>> GetByUserIdAsync(Guid userId)
    {
        var client = await _context.Clients
            .Include(c => c.Reclamations)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (client == null)
        {
            return ApiResponse<ClientDto>.FailureResponse("Client not found");
        }

        return ApiResponse<ClientDto>.SuccessResponse(MapToDto(client));
    }

    public async Task<ApiResponse<PaginatedResult<ClientDto>>> GetAllAsync(PaginationParams pagination)
    {
        var query = _context.Clients
            .Include(c => c.Reclamations)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            var searchLower = pagination.SearchTerm.ToLower();
            query = query.Where(c =>
                c.FullName.ToLower().Contains(searchLower) ||
                c.Email.ToLower().Contains(searchLower) ||
                c.Phone.Contains(searchLower));
        }

        var totalCount = await query.CountAsync();

        query = pagination.SortBy?.ToLower() switch
        {
            "name" => pagination.SortDescending ? query.OrderByDescending(c => c.FullName) : query.OrderBy(c => c.FullName),
            "email" => pagination.SortDescending ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
            _ => query.OrderByDescending(c => c.CreatedAt)
        };

        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var dtos = items.Select(MapToDto).ToList();
        var result = new PaginatedResult<ClientDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize);
        return ApiResponse<PaginatedResult<ClientDto>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<ClientDto>> CreateAsync(CreateClientRequest request)
    {
        var existingClient = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == request.UserId);
        if (existingClient != null)
        {
            return ApiResponse<ClientDto>.FailureResponse("Client profile already exists for this user");
        }

        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            City = request.City,
            CreatedAt = DateTime.UtcNow
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Client {ClientId} created for user {UserId}", client.Id, request.UserId);
        return ApiResponse<ClientDto>.SuccessResponse(MapToDto(client), "Client created successfully");
    }

    public async Task<ApiResponse<ClientDto>> UpdateAsync(Guid id, UpdateClientRequest request)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null)
        {
            return ApiResponse<ClientDto>.FailureResponse("Client not found");
        }

        client.FullName = request.FullName;
        client.Phone = request.Phone;
        client.Address = request.Address;
        client.City = request.City;
        client.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Client {ClientId} updated", id);
        return await GetByIdAsync(id);
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null)
        {
            return ApiResponse.FailureResponse("Client not found");
        }

        client.IsDeleted = true;
        client.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Client {ClientId} soft deleted", id);
        return ApiResponse.SuccessResponse("Client deleted successfully");
    }

    private static ClientDto MapToDto(Client client)
    {
        return new ClientDto
        {
            Id = client.Id,
            UserId = client.UserId,
            FullName = client.FullName,
            Email = client.Email,
            Phone = client.Phone,
            Address = client.Address,
            City = client.City,
            TotalReclamations = client.Reclamations.Count,
            ActiveReclamations = client.Reclamations.Count(r => r.Status != Shared.Enums.ReclamationStatus.Closed && r.Status != Shared.Enums.ReclamationStatus.Cancelled),
            IsDeleted = client.IsDeleted,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt
        };
    }
}

