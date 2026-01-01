using Savora.AuthService.Domain.Entities;

namespace Savora.AuthService.Application.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    bool ValidateRefreshToken(User user, string refreshToken);
}

