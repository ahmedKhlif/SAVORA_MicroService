using Microsoft.EntityFrameworkCore;
using Savora.ReclamationsService.Domain.Entities;
using Savora.ReclamationsService.Infrastructure.Data;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Dashboard;
using Savora.Shared.Enums;

namespace Savora.ReclamationsService.Application.Services;

public class DashboardServiceImpl : IDashboardService
{
    private readonly ReclamationsDbContext _context;
    private readonly IInterventionApiClient _interventionApiClient;
    private readonly IPartApiClient _partApiClient;
    private readonly IArticleApiClient _articleApiClient;
    private readonly ILogger<DashboardServiceImpl> _logger;

    public DashboardServiceImpl(
        ReclamationsDbContext context,
        IInterventionApiClient interventionApiClient,
        IPartApiClient partApiClient,
        IArticleApiClient articleApiClient,
        ILogger<DashboardServiceImpl> logger)
    {
        _context = context;
        _interventionApiClient = interventionApiClient;
        _partApiClient = partApiClient;
        _articleApiClient = articleApiClient;
        _logger = logger;
    }

    public async Task<ApiResponse<SavDashboardDto>> GetSavDashboardAsync()
    {
        try
        {
            var dashboard = new SavDashboardDto();

            // Reclamation Statistics
            var reclamations = await _context.Reclamations.ToListAsync();
            dashboard.TotalReclamations = reclamations.Count;
            dashboard.NewReclamations = reclamations.Count(r => r.Status == ReclamationStatus.New);
            dashboard.InProgressReclamations = reclamations.Count(r => r.Status == ReclamationStatus.InProgress);
            dashboard.ClosedReclamations = reclamations.Count(r => r.Status == ReclamationStatus.Closed);
            
            // Overdue reclamations (past SLA deadline)
            var now = DateTime.UtcNow;
            dashboard.OverdueReclamations = reclamations.Count(r => 
                r.SlaDeadline.HasValue && 
                r.SlaDeadline.Value < now && 
                r.Status != ReclamationStatus.Closed);

            // Average resolution days
            var closedReclamations = reclamations.Where(r => r.Status == ReclamationStatus.Closed && r.ClosedAt.HasValue).ToList();
            if (closedReclamations.Any())
            {
                dashboard.AverageResolutionDays = closedReclamations
                    .Average(r => (r.ClosedAt!.Value - r.CreatedAt).TotalDays);
            }

            // Performance Metrics
            dashboard.ResolutionRate = dashboard.TotalReclamations > 0 
                ? (double)dashboard.ClosedReclamations / dashboard.TotalReclamations * 100 
                : 0;
            
            // SLA Compliance Rate (reclamations closed within SLA deadline)
            var slaCompliantReclamations = closedReclamations.Count(r => 
                r.SlaDeadline.HasValue && r.ClosedAt.HasValue && r.ClosedAt.Value <= r.SlaDeadline.Value);
            dashboard.SlaComplianceRate = closedReclamations.Any() 
                ? (double)slaCompliantReclamations / closedReclamations.Count * 100 
                : 0;

            // Time-based statistics
            var today = DateTime.UtcNow.Date;
            var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
            var thisMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            
            dashboard.TodayReclamations = reclamations.Count(r => r.CreatedAt.Date == today);
            dashboard.ThisWeekReclamations = reclamations.Count(r => r.CreatedAt >= thisWeekStart);
            dashboard.ThisMonthReclamations = reclamations.Count(r => r.CreatedAt >= thisMonthStart);

            // Reclamations by Status
            dashboard.ReclamationsByStatus = reclamations
                .GroupBy(r => r.Status.ToString())
                .Select(g => new StatusCountDto { Status = g.Key, Count = g.Count() })
                .ToList();

            // Reclamations by Priority
            dashboard.ReclamationsByPriority = reclamations
                .GroupBy(r => r.Priority.ToString())
                .Select(g => new PriorityCountDto { Priority = g.Key, Count = g.Count() })
                .ToList();

            // Monthly Reclamations (last 12 months)
            var last12Months = Enumerable.Range(0, 12)
                .Select(i => DateTime.UtcNow.AddMonths(-i))
                .Reverse()
                .ToList();

            dashboard.MonthlyReclamations = last12Months.Select(month => new MonthlyDataDto
            {
                Month = month.ToString("MMM"),
                Year = month.Year,
                Count = reclamations.Count(r => r.CreatedAt.Year == month.Year && r.CreatedAt.Month == month.Month)
            }).ToList();

            // Initialize Monthly Revenue Data with zeros (will be updated if interventions exist)
            dashboard.MonthlyRevenueData = last12Months.Select(month => new MonthlyDataDto
            {
                Month = month.ToString("MMM"),
                Year = month.Year,
                Amount = 0
            }).ToList();

            // Fetch Interventions Data
            try
            {
                var interventionsResponse = await _interventionApiClient.GetAllInterventionsAsync();
                if (interventionsResponse?.Success == true && interventionsResponse.Data != null)
                {
                    var interventions = interventionsResponse.Data.Items;

                    // Calculate revenue from completed paid interventions (only completed interventions generate revenue)
                    var completedPaidInterventions = interventions
                        .Where(i => i.Status == InterventionStatus.Completed && !i.IsFree)
                        .ToList();
                    
                    dashboard.TotalInterventions = interventions.Count;
                    dashboard.PlannedInterventions = interventions.Count(i => i.Status == InterventionStatus.Planned);
                    dashboard.InProgressInterventions = interventions.Count(i => i.Status == InterventionStatus.InProgress);
                    dashboard.CompletedInterventions = interventions.Count(i => i.Status == InterventionStatus.Completed);
                    dashboard.CancelledInterventions = interventions.Count(i => i.Status == InterventionStatus.Cancelled);
                    dashboard.FreeInterventions = interventions.Count(i => i.IsFree);
                    dashboard.PaidInterventions = interventions.Count(i => !i.IsFree);
                    
                    dashboard.TodayInterventions = interventions.Count(i => i.PlannedDate.Date == today);
                    dashboard.ThisWeekInterventions = interventions.Count(i => i.PlannedDate >= thisWeekStart);

                    dashboard.TotalRevenue = completedPaidInterventions.Sum(i => i.TotalAmount);
                    dashboard.CurrentMonthRevenue = completedPaidInterventions
                        .Where(i => i.CreatedAt.Month == DateTime.UtcNow.Month && i.CreatedAt.Year == DateTime.UtcNow.Year)
                        .Sum(i => i.TotalAmount);
                    
                    // Last month revenue for growth calculation
                    var lastMonth = DateTime.UtcNow.AddMonths(-1);
                    dashboard.LastMonthRevenue = completedPaidInterventions
                        .Where(i => i.CreatedAt.Month == lastMonth.Month && i.CreatedAt.Year == lastMonth.Year)
                        .Sum(i => i.TotalAmount);
                    
                    // Revenue growth percentage
                    dashboard.RevenueGrowth = dashboard.LastMonthRevenue > 0
                        ? ((dashboard.CurrentMonthRevenue - dashboard.LastMonthRevenue) / dashboard.LastMonthRevenue) * 100
                        : 0;
                    
                    // Average intervention amount
                    dashboard.AverageInterventionAmount = completedPaidInterventions.Any()
                        ? completedPaidInterventions.Average(i => i.TotalAmount)
                        : 0;
                    
                    // Monthly Interventions Data
                    dashboard.MonthlyInterventionsData = last12Months.Select(month => new MonthlyDataDto
                    {
                        Month = month.ToString("MMM"),
                        Year = month.Year,
                        Count = interventions.Count(i => i.CreatedAt.Year == month.Year && i.CreatedAt.Month == month.Month)
                    }).ToList();

                    // Monthly Revenue Data (only from completed interventions)
                    dashboard.MonthlyRevenueData = last12Months.Select(month => new MonthlyDataDto
                    {
                        Month = month.ToString("MMM"),
                        Year = month.Year,
                        Amount = completedPaidInterventions
                            .Where(i => i.CreatedAt.Year == month.Year && i.CreatedAt.Month == month.Month)
                            .Sum(i => i.TotalAmount)
                    }).ToList();
                    
                    _logger.LogInformation("Successfully fetched {Count} interventions for dashboard", interventions.Count);
                }
                else
                {
                    _logger.LogWarning("Failed to fetch interventions: {Message}", interventionsResponse?.Message ?? "Unknown error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch interventions data for dashboard");
                dashboard.TotalInterventions = 0;
                dashboard.PlannedInterventions = 0;
                dashboard.InProgressInterventions = 0;
                dashboard.CompletedInterventions = 0;
                dashboard.CancelledInterventions = 0;
                dashboard.FreeInterventions = 0;
                dashboard.PaidInterventions = 0;
                dashboard.TodayInterventions = 0;
                dashboard.ThisWeekInterventions = 0;
                dashboard.CurrentMonthRevenue = 0;
                dashboard.LastMonthRevenue = 0;
                dashboard.TotalRevenue = 0;
                dashboard.RevenueGrowth = 0;
                dashboard.AverageInterventionAmount = 0;
                dashboard.MonthlyInterventionsData = last12Months.Select(month => new MonthlyDataDto
                {
                    Month = month.ToString("MMM"),
                    Year = month.Year,
                    Count = 0
                }).ToList();
                // MonthlyRevenueData already initialized above with zeros
            }

            // Fetch Articles Data
            try
            {
                var articlesResponse = await _articleApiClient.GetAllArticlesAsync();
                if (articlesResponse?.Success == true && articlesResponse.Data != null)
                {
                    var articles = articlesResponse.Data.Items;
                    dashboard.TotalArticles = articles.Count;
                    
                    // Articles by Category
                    dashboard.ArticlesByCategory = articles
                        .Where(a => !string.IsNullOrEmpty(a.Category))
                        .GroupBy(a => a.Category!)
                        .Select(g => new CategoryCountDto { Category = g.Key, Count = g.Count() })
                        .OrderByDescending(c => c.Count)
                        .ToList();
                    
                    // Reclamations by Article Category
                    // We need to fetch article details to get categories
                    // For now, we'll use a simplified approach
                    dashboard.ReclamationsByArticleCategory = new List<CategoryCountDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch articles data for dashboard");
                dashboard.TotalArticles = 0;
                dashboard.ArticlesByCategory = new List<CategoryCountDto>();
            }

            // Clients Statistics
            try
            {
                var clients = await _context.Clients.ToListAsync();
                dashboard.TotalClients = clients.Count;
                
                var activeClientIds = reclamations
                    .Where(r => r.Status != ReclamationStatus.Closed)
                    .Select(r => r.ClientId)
                    .Distinct()
                    .ToList();
                dashboard.ActiveClients = activeClientIds.Count;
                
                // Top Clients by Reclamations
                dashboard.TopClients = reclamations
                    .GroupBy(r => r.ClientId)
                    .Select(g => new TopClientDto
                    {
                        ClientId = g.Key,
                        ClientName = clients.FirstOrDefault(c => c.Id == g.Key)?.FullName ?? "Client inconnu",
                        ReclamationsCount = g.Count(),
                        ActiveReclamationsCount = g.Count(r => r.Status != ReclamationStatus.Closed)
                    })
                    .OrderByDescending(c => c.ReclamationsCount)
                    .Take(5)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate client statistics");
                dashboard.TotalClients = 0;
                dashboard.ActiveClients = 0;
                dashboard.TopClients = new List<TopClientDto>();
            }

            // Fetch Low Stock Parts
            try
            {
                var parts = await _partApiClient.GetAllPartsAsync();
                dashboard.TotalParts = parts.Count;
                dashboard.LowStockParts = parts
                    .Where(p => p.StockQuantity < p.MinStockLevel)
                    .Select(p => new LowStockPartDto
                    {
                        PartId = p.Id,
                        PartName = p.Name,
                        PartReference = p.Reference,
                        CurrentStock = p.StockQuantity,
                        MinStockLevel = p.MinStockLevel
                    })
                    .ToList();
                dashboard.LowStockPartsCount = dashboard.LowStockParts.Count;
                dashboard.OutOfStockPartsCount = parts.Count(p => p.StockQuantity == 0);
                
                // Total inventory value
                dashboard.TotalPartsValue = parts.Sum(p => p.StockQuantity * p.UnitPrice);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch low stock parts data for dashboard");
                dashboard.LowStockParts = new List<LowStockPartDto>();
                dashboard.TotalParts = 0;
                dashboard.LowStockPartsCount = 0;
                dashboard.OutOfStockPartsCount = 0;
                dashboard.TotalPartsValue = 0;
            }

            // Technician Workloads (would need a technicians endpoint)
            dashboard.TechnicianWorkloads = new List<TechnicianWorkloadDto>();

            return ApiResponse<SavDashboardDto>.SuccessResponse(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SAV dashboard");
            return ApiResponse<SavDashboardDto>.FailureResponse("Erreur lors du chargement du tableau de bord");
        }
    }

    public async Task<ApiResponse<ClientDashboardDto>> GetClientDashboardAsync(Guid userId)
    {
        try
        {
            var dashboard = new ClientDashboardDto();

            // Get client by user ID
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);
            
            if (client == null)
            {
                return ApiResponse<ClientDashboardDto>.FailureResponse("Client not found");
            }

            // Get client's reclamations
            var reclamations = await _context.Reclamations
                .Where(r => r.ClientId == client.Id)
                .ToListAsync();

            dashboard.TotalReclamations = reclamations.Count;
            dashboard.ActiveReclamations = reclamations.Count(r => 
                r.Status != ReclamationStatus.Closed);
            dashboard.ClosedReclamations = reclamations.Count(r => 
                r.Status == ReclamationStatus.Closed);

            // Get client's articles count
            try
            {
                var clientArticles = reclamations.Select(r => r.ClientArticleId).Distinct().ToList();
                dashboard.TotalArticles = clientArticles.Count;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate client articles count");
                dashboard.TotalArticles = 0;
            }

            // Get unread notifications (Notification uses UserId, not ClientId)
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
            dashboard.UnreadNotifications = notifications;

            // Recent Reclamations
            dashboard.RecentReclamations = reclamations
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r => new ReclamationSummaryDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    Status = r.Status.ToString(),
                    Priority = r.Priority.ToString(),
                    CreatedAt = r.CreatedAt
                })
                .ToList();

            // Upcoming Interventions - fetch from InterventionsService
            try
            {
                var clientReclamationIds = reclamations.Select(r => r.Id).ToList();
                var allInterventions = await _interventionApiClient.GetAllInterventionsAsync();
                if (allInterventions?.Success == true && allInterventions.Data != null)
                {
                    var clientInterventions = allInterventions.Data.Items
                        .Where(i => clientReclamationIds.Contains(i.ReclamationId))
                        .Where(i => i.PlannedDate > DateTime.UtcNow && i.Status == InterventionStatus.Planned)
                        .OrderBy(i => i.PlannedDate)
                        .Take(5)
                        .Select(i => new InterventionSummaryDto
                        {
                            Id = i.Id,
                            ReclamationTitle = i.ReclamationTitle,
                            TechnicianName = i.TechnicianName ?? "N/A",
                            PlannedDate = i.PlannedDate,
                            Status = i.Status.ToString()
                        })
                        .ToList();
                    dashboard.UpcomingInterventions = clientInterventions;
                }
                else
                {
                    dashboard.UpcomingInterventions = new List<InterventionSummaryDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch upcoming interventions for client dashboard");
                dashboard.UpcomingInterventions = new List<InterventionSummaryDto>();
            }

            return ApiResponse<ClientDashboardDto>.SuccessResponse(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting client dashboard for {UserId}", userId);
            return ApiResponse<ClientDashboardDto>.FailureResponse("Erreur lors du chargement du tableau de bord");
        }
    }
}

