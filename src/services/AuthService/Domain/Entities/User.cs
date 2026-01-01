using Savora.Shared.Enums;

namespace Savora.AuthService.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsBlocked { get; set; } = false;
    public DateTime? BlockedAt { get; set; }
    public string? BlockReason { get; set; }
    public bool IsDeleted { get; set; }
    
    // Email Verification
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }
    
    // Password Reset
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    
    // Refresh Token
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? ProfilePictureUrl { get; set; }

    // Navigation
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

