using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Savora.AuthService.Application.Services;
using Savora.Shared.DTOs.Auth;
using Savora.Shared.DTOs.Common;
using Savora.Shared.Enums;

namespace Savora.AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        if (!result.Success)
        {
            return Unauthorized(result);
        }
        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _authService.LogoutAsync(userId.Value);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _authService.GetUserAsync(userId.Value);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _authService.UpdateUserAsync(userId.Value, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [Authorize]
    [HttpPost("me/upload-picture")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse.FailureResponse("No file uploaded"));
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest(ApiResponse.FailureResponse("Invalid file type. Only JPG, PNG, and GIF are allowed."));
        }

        // Validate file size (max 5MB)
        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(ApiResponse.FailureResponse("File size exceeds 5MB limit"));
        }

        try
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"/uploads/profiles/{fileName}";
            var result = await _authService.UpdateProfilePictureAsync(userId.Value, fileUrl);
            
            if (!result.Success)
            {
                // Delete uploaded file if update failed
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.FailureResponse($"Error uploading file: {ex.Message}"));
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _authService.ChangePasswordAsync(userId.Value, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [Authorize(Roles = "ResponsableSAV")]
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var result = await _authService.GetAllUsersAsync();
        return Ok(result);
    }

    [Authorize]
    [HttpGet("users/for-messages")]
    public async Task<IActionResult> GetUsersForMessages()
    {
        var result = await _authService.GetAllUsersAsync();
        if (!result.Success)
        {
            return BadRequest(result);
        }
        
        // Return only basic info needed for messages (Id, FullName, Email, ProfilePictureUrl)
        var usersForMessages = result.Data?.Select(u => new UserDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            ProfilePictureUrl = u.ProfilePictureUrl,
            Role = u.Role
        }).ToList();
        
        return Ok(ApiResponse<List<UserDto>>.SuccessResponse(usersForMessages ?? new List<UserDto>()));
    }

    [Authorize]
    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var result = await _authService.GetUserAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        
        // For non-admin users, return only basic info needed for messages
        if (!User.IsInRole("ResponsableSAV"))
        {
            var basicUser = new UserDto
            {
                Id = result.Data!.Id,
                FullName = result.Data.FullName,
                Email = result.Data.Email,
                ProfilePictureUrl = result.Data.ProfilePictureUrl,
                Role = result.Data.Role
            };
            return Ok(ApiResponse<UserDto>.SuccessResponse(basicUser));
        }
        
        return Ok(result);
    }

    [Authorize(Roles = "ResponsableSAV")]
    [HttpPost("users/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var result = await _authService.DeactivateUserAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [Authorize(Roles = "ResponsableSAV")]
    [HttpPost("users/{id:guid}/activate")]
    public async Task<IActionResult> ActivateUser(Guid id)
    {
        var result = await _authService.ActivateUserAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [Authorize(Roles = "ResponsableSAV")]
    [HttpPost("users/{id:guid}/block")]
    public async Task<IActionResult> BlockUser(Guid id, [FromBody] BlockUserRequest? request = null)
    {
        var result = await _authService.BlockUserAsync(id, request?.Reason);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [Authorize(Roles = "ResponsableSAV")]
    [HttpPost("users/{id:guid}/unblock")]
    public async Task<IActionResult> UnblockUser(Guid id)
    {
        var result = await _authService.UnblockUserAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [Authorize(Roles = "ResponsableSAV")]
    [HttpPut("users/{id:guid}/role")]
    public async Task<IActionResult> ChangeUserRole(Guid id, [FromBody] ChangeRoleRequest request)
    {
        if (!Enum.TryParse<UserRole>(request.Role, out var newRole))
        {
            return BadRequest(ApiResponse.FailureResponse("Invalid role"));
        }

        var result = await _authService.ChangeUserRoleAsync(id, newRole);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [Authorize(Roles = "ResponsableSAV")]
    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var result = await _authService.DeleteUserAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    // Email Verification Endpoints
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var result = await _authService.VerifyEmailAsync(token);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendVerificationRequest request)
    {
        var result = await _authService.ResendVerificationEmailAsync(request.Email);
        return Ok(result);
    }

    // Password Reset Endpoints
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request.Email);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class ResendVerificationRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class BlockUserRequest
{
    public string? Reason { get; set; }
}

public class ChangeRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

