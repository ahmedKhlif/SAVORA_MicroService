namespace Savora.Shared.DTOs.Dashboard;

// Alias for API response compatibility
public class DashboardStatsDto : SavDashboardDto { }
public class ClientDashboardStatsDto : ClientDashboardDto { }

public class ClientDashboardDto
{
    public int TotalReclamations { get; set; }
    public int ActiveReclamations { get; set; }
    public int ClosedReclamations { get; set; }
    public int TotalArticles { get; set; }
    public int UnreadNotifications { get; set; }
    public List<ReclamationSummaryDto> RecentReclamations { get; set; } = new();
    public List<InterventionSummaryDto> UpcomingInterventions { get; set; } = new();
}

public class SavDashboardDto
{
    // KPIs
    public int TotalReclamations { get; set; }
    public int NewReclamations { get; set; }
    public int InProgressReclamations { get; set; }
    public int ClosedReclamations { get; set; }
    public int OverdueReclamations { get; set; }
    public double AverageResolutionDays { get; set; }
    
    // Performance Metrics
    public double ResolutionRate { get; set; } // Percentage of closed vs total
    public double SlaComplianceRate { get; set; } // Percentage of reclamations resolved within SLA
    public int TodayReclamations { get; set; }
    public int ThisWeekReclamations { get; set; }
    public int ThisMonthReclamations { get; set; }
    public decimal AverageInterventionAmount { get; set; }
    
    // Interventions
    public int TotalInterventions { get; set; }
    public int PlannedInterventions { get; set; }
    public int InProgressInterventions { get; set; }
    public int CompletedInterventions { get; set; }
    public int CancelledInterventions { get; set; }
    public int FreeInterventions { get; set; }
    public int PaidInterventions { get; set; }
    public int TodayInterventions { get; set; }
    public int ThisWeekInterventions { get; set; }
    
    // Revenue
    public decimal CurrentMonthRevenue { get; set; }
    public decimal LastMonthRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RevenueGrowth { get; set; } // Percentage change from last month
    
    // Articles & Clients
    public int TotalArticles { get; set; }
    public int TotalClients { get; set; }
    public int ActiveClients { get; set; } // Clients with active reclamations
    public List<CategoryCountDto> ArticlesByCategory { get; set; } = new();
    public List<CategoryCountDto> ReclamationsByArticleCategory { get; set; } = new();
    
    // Parts
    public int TotalParts { get; set; }
    public int LowStockPartsCount { get; set; }
    public int OutOfStockPartsCount { get; set; }
    public decimal TotalPartsValue { get; set; } // Total inventory value
    
    // Charts data
    public List<StatusCountDto> ReclamationsByStatus { get; set; } = new();
    public List<PriorityCountDto> ReclamationsByPriority { get; set; } = new();
    public List<MonthlyDataDto> MonthlyReclamations { get; set; } = new();
    public List<MonthlyDataDto> MonthlyRevenueData { get; set; } = new();
    public List<MonthlyDataDto> MonthlyInterventionsData { get; set; } = new();
    public List<TechnicianWorkloadDto> TechnicianWorkloads { get; set; } = new();
    public List<LowStockPartDto> LowStockParts { get; set; } = new();
    public List<TopClientDto> TopClients { get; set; } = new(); // Top clients by reclamations
}

public class ReclamationSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class InterventionSummaryDto
{
    public Guid Id { get; set; }
    public string ReclamationTitle { get; set; } = string.Empty;
    public string TechnicianName { get; set; } = string.Empty;
    public DateTime PlannedDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class StatusCountDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PriorityCountDto
{
    public string Priority { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class MonthlyDataDto
{
    public string Month { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public class TechnicianWorkloadDto
{
    public Guid TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public int ActiveInterventions { get; set; }
    public int CompletedThisMonth { get; set; }
    public bool IsAvailable { get; set; }
}

public class LowStockPartDto
{
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartReference { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinStockLevel { get; set; }
}

public class CategoryCountDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopClientDto
{
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int ReclamationsCount { get; set; }
    public int ActiveReclamationsCount { get; set; }
}

