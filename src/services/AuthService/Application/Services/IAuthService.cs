using Savora.Shared.DTOs.Auth;
using Savora.Shared.DTOs.Common;
using Savora.Shared.Enums;

namespace Savora.AuthService.Application.Services;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<LoginResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<LoginResponse>> RefreshTokenAsync(string refreshToken);
    Task<ApiResponse> LogoutAsync(Guid userId);
    Task<ApiResponse<UserDto>> GetUserAsync(Guid userId);
    Task<ApiResponse<List<UserDto>>> GetAllUsersAsync();
    Task<ApiResponse<UserDto>> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task<ApiResponse<UserDto>> UpdateProfilePictureAsync(Guid userId, string profilePictureUrl);
    Task<ApiResponse> DeactivateUserAsync(Guid userId);
    Task<ApiResponse> ActivateUserAsync(Guid userId);
    Task<ApiResponse> BlockUserAsync(Guid userId, string? reason = null);
    Task<ApiResponse> UnblockUserAsync(Guid userId);
    Task<ApiResponse<UserDto>> ChangeUserRoleAsync(Guid userId, UserRole newRole);
    Task<ApiResponse> DeleteUserAsync(Guid userId);
    Task<ApiResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    
    // Email Verification
    Task<ApiResponse> VerifyEmailAsync(string token);
    Task<ApiResponse> ResendVerificationEmailAsync(string email);
    
    // Password Reset
    Task<ApiResponse> ForgotPasswordAsync(string email);
    Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request);
}

public class UpdateUserRequest
{
    public string FullName { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class BlockUserRequest
{
    public string? Reason { get; set; }
}

public class ChangeRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

