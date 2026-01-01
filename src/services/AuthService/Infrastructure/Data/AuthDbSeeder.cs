using Microsoft.EntityFrameworkCore;
using Savora.AuthService.Domain.Entities;
using Savora.Shared.Enums;

namespace Savora.AuthService.Infrastructure.Data;

public static class AuthDbSeeder
{
    public static async Task SeedAsync(AuthDbContext context)
    {
        try
        {
            if (!await context.Users.AnyAsync())
            {
            var users = new List<User>
            {
                new User
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Email = "admin@savora.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    FullName = "Responsable SAV",
                    Role = UserRole.ResponsableSAV,
                    IsActive = true,
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Email = "client@savora.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Client@123"),
                    FullName = "Ahmed Ben Ali",
                    Role = UserRole.Client,
                    IsActive = true,
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Email = "client2@savora.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Client@123"),
                    FullName = "Fatima Trabelsi",
                    Role = UserRole.Client,
                    IsActive = true,
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

                context.Users.AddRange(users);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception)
        {
            // Table might not exist yet, ignore and retry later
            throw;
        }
    }
}

