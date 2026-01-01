using Microsoft.EntityFrameworkCore;
using Savora.AuthService.Domain.Entities;
using Savora.AuthService.Infrastructure.Data;
using Savora.Shared.DTOs.Auth;
using Savora.Shared.DTOs.Common;
using Savora.Shared.Enums;

namespace Savora.AuthService.Application.Services;

public class AuthServiceImpl : IAuthService
{
    private readonly AuthDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthServiceImpl> _logger;

    public AuthServiceImpl(
        AuthDbContext context,
        IJwtService jwtService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthServiceImpl> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (user == null)
            {
                return ApiResponse<LoginResponse>.FailureResponse("Email ou mot de passe invalide");
            }

            if (!user.IsActive)
            {
                return ApiResponse<LoginResponse>.FailureResponse("Ce compte est désactivé");
            }

            if (user.IsBlocked)
            {
                return ApiResponse<LoginResponse>.FailureResponse("Votre compte a été bloqué. Veuillez contacter l'administrateur.");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return ApiResponse<LoginResponse>.FailureResponse("Email ou mot de passe invalide");
            }

            if (!user.IsEmailVerified)
            {
                return ApiResponse<LoginResponse>.FailureResponse("Veuillez vérifier votre email avant de vous connecter. Consultez votre boîte de réception.");
            }

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var refreshTokenExpiryDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);
            user.LastLoginAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new LoginResponse
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "60")),
                User = MapToDto(user)
            };

            _logger.LogInformation("User {Email} logged in successfully", user.Email);
            return ApiResponse<LoginResponse>.SuccessResponse(response, "Connexion réussie");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return ApiResponse<LoginResponse>.FailureResponse("Une erreur s'est produite lors de la connexion");
        }
    }

    public async Task<ApiResponse<LoginResponse>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                return ApiResponse<LoginResponse>.FailureResponse("Cet email est déjà utilisé");
            }

            // Generate email verification token
            var verificationToken = GenerateSecureToken();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                Role = UserRole.Client,
                IsActive = true,
                IsEmailVerified = false,
                EmailVerificationToken = verificationToken,
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Send verification email
            try
            {
                await _emailService.SendEmailVerificationAsync(user.Email, user.FullName, verificationToken);
                _logger.LogInformation("Verification email sent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send verification email to {Email}", user.Email);
            }

            // Return success but user needs to verify email before login
            var response = new LoginResponse
            {
                Token = string.Empty, // No token until email verified
                RefreshToken = string.Empty,
                ExpiresAt = DateTime.UtcNow,
                User = MapToDto(user)
            };

            _logger.LogInformation("User {Email} registered successfully, awaiting email verification", user.Email);
            return ApiResponse<LoginResponse>.SuccessResponse(response, "Inscription réussie! Veuillez vérifier votre email pour activer votre compte.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", request.Email);
            return ApiResponse<LoginResponse>.FailureResponse("Une erreur s'est produite lors de l'inscription");
        }
    }

    private static string GenerateSecureToken()
    {
        return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    public async Task<ApiResponse<LoginResponse>> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null || !_jwtService.ValidateRefreshToken(user, refreshToken))
            {
                return ApiResponse<LoginResponse>.FailureResponse("Invalid refresh token");
            }

            var accessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            var refreshTokenExpiryDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);
            await _context.SaveChangesAsync();

            var response = new LoginResponse
            {
                Token = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "60")),
                User = MapToDto(user)
            };

            return ApiResponse<LoginResponse>.SuccessResponse(response, "Token refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return ApiResponse<LoginResponse>.FailureResponse("An error occurred while refreshing token");
        }
    }

    public async Task<ApiResponse> LogoutAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return ApiResponse.FailureResponse("User not found");
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} logged out", userId);
            return ApiResponse.SuccessResponse("Logout successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return ApiResponse.FailureResponse("An error occurred during logout");
        }
    }

    public async Task<ApiResponse<UserDto>> GetUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return ApiResponse<UserDto>.FailureResponse("User not found");
        }

        return ApiResponse<UserDto>.SuccessResponse(MapToDto(user));
    }

    public async Task<ApiResponse<List<UserDto>>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => MapToDto(u))
            .ToListAsync();

        return ApiResponse<List<UserDto>>.SuccessResponse(users);
    }

    public async Task<ApiResponse<UserDto>> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return ApiResponse<UserDto>.FailureResponse("User not found");
        }

        user.FullName = request.FullName;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated", userId);
        return ApiResponse<UserDto>.SuccessResponse(MapToDto(user), "User updated successfully");
    }

    public async Task<ApiResponse<UserDto>> UpdateProfilePictureAsync(Guid userId, string profilePictureUrl)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return ApiResponse<UserDto>.FailureResponse("User not found");
        }

        // Delete old profile picture if exists
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            try
            {
                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePictureUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete old profile picture for user {UserId}", userId);
            }
        }

        user.ProfilePictureUrl = profilePictureUrl;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Profile picture updated for user {UserId}", userId);
        return ApiResponse<UserDto>.SuccessResponse(MapToDto(user), "Profile picture updated successfully");
    }

    public async Task<ApiResponse> DeactivateUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return ApiResponse.FailureResponse("User not found");
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deactivated", userId);
        return ApiResponse.SuccessResponse("User deactivated successfully");
    }

    public async Task<ApiResponse> ActivateUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return ApiResponse.FailureResponse("User not found");
        }

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} activated", userId);
        return ApiResponse.SuccessResponse("User activated successfully");
    }

    public async Task<ApiResponse> BlockUserAsync(Guid userId, string? reason = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return ApiResponse.FailureResponse("User not found");
        }

        if (user.IsBlocked)
        {
            return ApiResponse.FailureResponse("User is already blocked");
        }

        user.IsBlocked = true;
        user.BlockedAt = DateTime.UtcNow;
        user.BlockReason = reason;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Send block notification email
        try
        {
            await _emailService.SendAccountBlockedEmailAsync(user.Email, user.FullName, reason);
            _logger.LogInformation("Block notification email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send block notification email to {Email}", user.Email);
        }

        _logger.LogInformation("User {UserId} blocked", userId);
        return ApiResponse.SuccessResponse("User blocked successfully");
    }

    public async Task<ApiResponse> UnblockUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return ApiResponse.FailureResponse("User not found");
        }

        if (!user.IsBlocked)
        {
            return ApiResponse.FailureResponse("User is not blocked");
        }

        user.IsBlocked = false;
        user.BlockedAt = null;
        user.BlockReason = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Send unblock notification email
        try
        {
            await _emailService.SendAccountUnblockedEmailAsync(user.Email, user.FullName);
            _logger.LogInformation("Unblock notification email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send unblock notification email to {Email}", user.Email);
        }

        _logger.LogInformation("User {UserId} unblocked", userId);
        return ApiResponse.SuccessResponse("User unblocked successfully");
    }

    public async Task<ApiResponse<UserDto>> ChangeUserRoleAsync(Guid userId, UserRole newRole)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return ApiResponse<UserDto>.FailureResponse("User not found");
        }

        user.Role = newRole;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} role changed to {Role}", userId, newRole);
        return ApiResponse<UserDto>.SuccessResponse(MapToDto(user), "User role updated successfully");
    }

    public async Task<ApiResponse> DeleteUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return ApiResponse.FailureResponse("User not found");
        }

        // Soft delete
        user.IsDeleted = true;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deleted", userId);
        return ApiResponse.SuccessResponse("User deleted successfully");
    }

    public async Task<ApiResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return ApiResponse.FailureResponse("Utilisateur non trouvé");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return ApiResponse.FailureResponse("Mot de passe actuel incorrect");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Send notification email
        try
        {
            await _emailService.SendPasswordChangedNotificationAsync(user.Email, user.FullName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password change notification to {Email}", user.Email);
        }

        _logger.LogInformation("Password changed for user {UserId}", userId);
        return ApiResponse.SuccessResponse("Mot de passe modifié avec succès");
    }

    public async Task<ApiResponse> VerifyEmailAsync(string token)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

        if (user == null)
        {
            return ApiResponse.FailureResponse("Token de vérification invalide");
        }

        if (user.EmailVerificationTokenExpiry < DateTime.UtcNow)
        {
            return ApiResponse.FailureResponse("Le token de vérification a expiré. Veuillez demander un nouveau lien.");
        }

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Send welcome email
        try
        {
            await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send welcome email to {Email}", user.Email);
        }

        _logger.LogInformation("Email verified for user {Email}", user.Email);
        return ApiResponse.SuccessResponse("Email vérifié avec succès! Vous pouvez maintenant vous connecter.");
    }

    public async Task<ApiResponse> ResendVerificationEmailAsync(string email)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        if (user == null)
        {
            // Don't reveal if email exists
            return ApiResponse.SuccessResponse("Si cet email existe, un nouveau lien de vérification a été envoyé.");
        }

        if (user.IsEmailVerified)
        {
            return ApiResponse.FailureResponse("Cet email est déjà vérifié");
        }

        // Generate new token
        user.EmailVerificationToken = GenerateSecureToken();
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.SendEmailVerificationAsync(user.Email, user.FullName, user.EmailVerificationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resend verification email to {Email}", user.Email);
        }

        return ApiResponse.SuccessResponse("Un nouveau lien de vérification a été envoyé à votre email.");
    }

    public async Task<ApiResponse> ForgotPasswordAsync(string email)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        if (user == null)
        {
            // Don't reveal if email exists for security
            return ApiResponse.SuccessResponse("Si cet email existe, un lien de réinitialisation a été envoyé.");
        }

        // Generate password reset token
        user.PasswordResetToken = GenerateSecureToken();
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.SendPasswordResetAsync(user.Email, user.FullName, user.PasswordResetToken);
            _logger.LogInformation("Password reset email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password reset email to {Email}", user.Email);
        }

        return ApiResponse.SuccessResponse("Si cet email existe, un lien de réinitialisation a été envoyé.");
    }

    public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token);

        if (user == null)
        {
            return ApiResponse.FailureResponse("Token de réinitialisation invalide");
        }

        if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
        {
            return ApiResponse.FailureResponse("Le token de réinitialisation a expiré. Veuillez faire une nouvelle demande.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Send notification email
        try
        {
            await _emailService.SendPasswordChangedNotificationAsync(user.Email, user.FullName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password change notification to {Email}", user.Email);
        }

        _logger.LogInformation("Password reset for user {Email}", user.Email);
        return ApiResponse.SuccessResponse("Mot de passe réinitialisé avec succès! Vous pouvez maintenant vous connecter.");
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            IsBlocked = user.IsBlocked,
            BlockedAt = user.BlockedAt,
            BlockReason = user.BlockReason,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            ProfilePictureUrl = user.ProfilePictureUrl
        };
    }
}

