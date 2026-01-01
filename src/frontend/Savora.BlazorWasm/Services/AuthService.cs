using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Savora.Shared.DTOs.Auth;
using Savora.Shared.DTOs.Common;

namespace Savora.BlazorWasm.Services;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<LoginResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse> LogoutAsync();
    Task<ApiResponse<UserDto>> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetUserRoleAsync();
    
    // Email Verification
    Task<ApiResponse<object>> VerifyEmailAsync(string token);
    Task<ApiResponse<object>> ResendVerificationEmailAsync(string email);
    
    // Password Reset
    Task<ApiResponse<object>> ForgotPasswordAsync(string email);
    Task<ApiResponse<object>> ResetPasswordAsync(string token, string newPassword);
    
    // Change Password
    Task<ApiResponse<object>> ChangePasswordAsync(string currentPassword, string newPassword);
    
    // Admin User Management
    Task<ApiResponse<List<UserDto>>> GetAllUsersAsync();
    Task<ApiResponse<List<UserDto>>> GetUsersForMessagesAsync();
    Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid userId);
    Task<ApiResponse> BlockUserAsync(Guid userId, string? reason = null);
    Task<ApiResponse> UnblockUserAsync(Guid userId);
    Task<ApiResponse<UserDto>> ChangeUserRoleAsync(Guid userId, string role);
    Task<ApiResponse> DeleteUserAsync(Guid userId);
    Task<ApiResponse> ActivateUserAsync(Guid userId);
    Task<ApiResponse> DeactivateUserAsync(Guid userId);
    
    // Profile Management
    Task<ApiResponse<UserDto>> UpdateProfileAsync(string fullName);
    Task<ApiResponse<UserDto>> UploadProfilePictureAsync(IBrowserFile file);
}

public class AuthService : IAuthService
{
    private readonly ApiHttpClient _apiClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;

    public AuthService(
        ApiHttpClient apiClient,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider)
    {
        _apiClient = apiClient;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var result = await _apiClient.PostAsync<LoginResponse>("auth", "/api/auth/login", request);
        
        if (result.Success && result.Data != null)
        {
            await _localStorage.SetItemAsync("authToken", result.Data.Token);
            await _localStorage.SetItemAsync("refreshToken", result.Data.RefreshToken);
            await _localStorage.SetItemAsync("user", result.Data.User);
            
            ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(result.Data.Token);
        }
        
        return result;
    }

    public async Task<ApiResponse<LoginResponse>> RegisterAsync(RegisterRequest request)
    {
        var result = await _apiClient.PostAsync<LoginResponse>("auth", "/api/auth/register", request);
        
        // Don't auto-login - user needs to verify email first
        // Token will be empty for unverified accounts
        
        return result;
    }

    public async Task<ApiResponse> LogoutAsync()
    {
        await _apiClient.PostAsync<object>("auth", "/api/auth/logout");
        
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("refreshToken");
        await _localStorage.RemoveItemAsync("user");
        
        ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();
        
        return ApiResponse.SuccessResponse("Logged out successfully");
    }

    public async Task<ApiResponse<UserDto>> GetCurrentUserAsync()
    {
        var user = await _localStorage.GetItemAsync<UserDto>("user");
        if (user != null)
        {
            return ApiResponse<UserDto>.SuccessResponse(user);
        }
        
        return await _apiClient.GetAsync<UserDto>("auth", "/api/auth/me");
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        return !string.IsNullOrEmpty(token);
    }

    public async Task<string?> GetUserRoleAsync()
    {
        var user = await _localStorage.GetItemAsync<UserDto>("user");
        return user?.Role;
    }

    public async Task<ApiResponse<object>> VerifyEmailAsync(string token)
    {
        return await _apiClient.GetAsync<object>("auth", $"/api/auth/verify-email?token={token}");
    }

    public async Task<ApiResponse<object>> ResendVerificationEmailAsync(string email)
    {
        return await _apiClient.PostAsync<object>("auth", "/api/auth/resend-verification", new { Email = email });
    }

    public async Task<ApiResponse<object>> ForgotPasswordAsync(string email)
    {
        return await _apiClient.PostAsync<object>("auth", "/api/auth/forgot-password", new { Email = email });
    }

    public async Task<ApiResponse<object>> ResetPasswordAsync(string token, string newPassword)
    {
        return await _apiClient.PostAsync<object>("auth", "/api/auth/reset-password", new { Token = token, NewPassword = newPassword });
    }

    public async Task<ApiResponse<object>> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        return await _apiClient.PostAsync<object>("auth", "/api/auth/change-password", new { CurrentPassword = currentPassword, NewPassword = newPassword });
    }

    // Admin User Management
    public async Task<ApiResponse<List<UserDto>>> GetAllUsersAsync()
    {
        return await _apiClient.GetAsync<List<UserDto>>("auth", "/api/auth/users");
    }

    public async Task<ApiResponse<List<UserDto>>> GetUsersForMessagesAsync()
    {
        return await _apiClient.GetAsync<List<UserDto>>("auth", "/api/auth/users/for-messages");
    }

    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid userId)
    {
        return await _apiClient.GetAsync<UserDto>("auth", $"/api/auth/users/{userId}");
    }

    public async Task<ApiResponse> BlockUserAsync(Guid userId, string? reason = null)
    {
        var result = await _apiClient.PostAsync<object>("auth", $"/api/auth/users/{userId}/block", new { Reason = reason });
        return new ApiResponse { Success = result.Success, Message = result.Message };
    }

    public async Task<ApiResponse> UnblockUserAsync(Guid userId)
    {
        var result = await _apiClient.PostAsync<object>("auth", $"/api/auth/users/{userId}/unblock", null);
        return new ApiResponse { Success = result.Success, Message = result.Message };
    }

    public async Task<ApiResponse<UserDto>> ChangeUserRoleAsync(Guid userId, string role)
    {
        return await _apiClient.PutAsync<UserDto>("auth", $"/api/auth/users/{userId}/role", new { Role = role });
    }

    public async Task<ApiResponse> DeleteUserAsync(Guid userId)
    {
        return await _apiClient.DeleteAsync("auth", $"/api/auth/users/{userId}");
    }

    public async Task<ApiResponse> ActivateUserAsync(Guid userId)
    {
        var result = await _apiClient.PostAsync<object>("auth", $"/api/auth/users/{userId}/activate", null);
        return new ApiResponse { Success = result.Success, Message = result.Message };
    }

    public async Task<ApiResponse> DeactivateUserAsync(Guid userId)
    {
        var result = await _apiClient.PostAsync<object>("auth", $"/api/auth/users/{userId}/deactivate", null);
        return new ApiResponse { Success = result.Success, Message = result.Message };
    }

    public async Task<ApiResponse<UserDto>> UpdateProfileAsync(string fullName)
    {
        return await _apiClient.PutAsync<UserDto>("auth", "/api/auth/me", new { FullName = fullName });
    }

    public async Task<ApiResponse<UserDto>> UploadProfilePictureAsync(Microsoft.AspNetCore.Components.Forms.IBrowserFile file)
    {
        using var content = new MultipartFormDataContent();
        using var fileStream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024); // 5MB max
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(streamContent, "file", file.Name);

        return await _apiClient.PostMultipartAsync<UserDto>("auth", "/api/auth/me/upload-picture", content);
    }
}

