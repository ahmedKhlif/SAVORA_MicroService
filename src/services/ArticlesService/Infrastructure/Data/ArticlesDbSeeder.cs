using Microsoft.EntityFrameworkCore;
using Savora.ArticlesService.Domain.Entities;
using Savora.Shared.Enums;

namespace Savora.ArticlesService.Infrastructure.Data;

public static class ArticlesDbSeeder
{
    public static async Task SeedAsync(ArticlesDbContext context)
    {
        // Seed Parts
        if (!context.Parts.Any())
        {
            var parts = new List<Part>
            {
                new Part
                {
                    Id = Guid.Parse("aaaa1111-1111-1111-1111-111111111111"),
                    Reference = "PART-001",
                    Name = "Thermostat Régulateur",
                    Description = "Thermostat de régulation pour chaudière",
                    UnitPrice = 45.00m,
                    StockQuantity = 25,
                    MinStockLevel = 5,
                    Category = "Chauffage",
                    CreatedAt = DateTime.UtcNow
                },
                new Part
                {
                    Id = Guid.Parse("aaaa2222-2222-2222-2222-222222222222"),
                    Reference = "PART-002",
                    Name = "Joint Robinet",
                    Description = "Joint d'étanchéité pour robinetterie",
                    UnitPrice = 8.50m,
                    StockQuantity = 100,
                    MinStockLevel = 20,
                    Category = "Sanitaire",
                    CreatedAt = DateTime.UtcNow
                },
                new Part
                {
                    Id = Guid.Parse("aaaa3333-3333-3333-3333-333333333333"),
                    Reference = "PART-003",
                    Name = "Pompe Circulateur",
                    Description = "Pompe de circulation pour système de chauffage",
                    UnitPrice = 180.00m,
                    StockQuantity = 8,
                    MinStockLevel = 3,
                    Category = "Chauffage",
                    CreatedAt = DateTime.UtcNow
                },
                new Part
                {
                    Id = Guid.Parse("aaaa4444-4444-4444-4444-444444444444"),
                    Reference = "PART-004",
                    Name = "Soupape Sécurité",
                    Description = "Soupape de sécurité 3 bars",
                    UnitPrice = 25.00m,
                    StockQuantity = 15,
                    MinStockLevel = 5,
                    Category = "Chauffage",
                    CreatedAt = DateTime.UtcNow
                },
                new Part
                {
                    Id = Guid.Parse("aaaa5555-5555-5555-5555-555555555555"),
                    Reference = "PART-005",
                    Name = "Mitigeur Lavabo",
                    Description = "Mitigeur monocommande pour lavabo",
                    UnitPrice = 75.00m,
                    StockQuantity = 12,
                    MinStockLevel = 4,
                    Category = "Sanitaire",
                    CreatedAt = DateTime.UtcNow
                },
                new Part
                {
                    Id = Guid.Parse("aaaa6666-6666-6666-6666-666666666666"),
                    Reference = "PART-006",
                    Name = "Brûleur Chaudière",
                    Description = "Brûleur de remplacement pour chaudière gaz",
                    UnitPrice = 320.00m,
                    StockQuantity = 3,
                    MinStockLevel = 2,
                    Category = "Chauffage",
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Parts.AddRange(parts);
            await context.SaveChangesAsync();
        }

        // Seed Articles - Client Articles Only (SAV Application)
        // Clear existing articles and seed fresh data (one-time operation)
        var hasArticles = await context.Articles.AnyAsync();
        if (hasArticles)
        {
            // Clear all articles to start fresh
            context.Articles.RemoveRange(context.Articles);
            await context.SaveChangesAsync();
        }

        // Seed articles only if table is empty
        if (!await context.Articles.AnyAsync())
        {
            var clientId = Guid.Parse("cccc1111-1111-1111-1111-111111111111"); // From ReclamationsService seed
            var clientId2 = Guid.Parse("cccc2222-2222-2222-2222-222222222222");

            var seedArticles = new List<Article>
        {
            new Article
            {
                Id = Guid.Parse("eeee1111-1111-1111-1111-111111111111"),
                Reference = "ART-001",
                Name = "Chaudière Gaz Condensation",
                Brand = "Viessmann",
                Category = "Chauffage Central",
                Price = 3500.00m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-6),
                WarrantyMonths = 24,
                SerialNumber = "VIE-2024-001234",
                ClientId = clientId,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            },
            new Article
            {
                Id = Guid.Parse("eeee2222-2222-2222-2222-222222222222"),
                Reference = "ART-002",
                Name = "Ballon Eau Chaude 200L",
                Brand = "Atlantic",
                Category = "Eau Chaude Sanitaire",
                Price = 850.00m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-18),
                WarrantyMonths = 24,
                SerialNumber = "ATL-2023-005678",
                ClientId = clientId,
                CreatedAt = DateTime.UtcNow.AddMonths(-18)
            },
            new Article
            {
                Id = Guid.Parse("eeee3333-3333-3333-3333-333333333333"),
                Reference = "ART-003",
                Name = "Radiateur Acier 1000W",
                Brand = "Acova",
                Category = "Radiateurs",
                Price = 120.00m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-30),
                WarrantyMonths = 12,
                SerialNumber = "ACO-2022-009012",
                ClientId = clientId,
                CreatedAt = DateTime.UtcNow.AddMonths(-30)
            },
            new Article
            {
                Id = Guid.Parse("eeee4444-4444-4444-4444-444444444444"),
                Reference = "ART-004",
                Name = "Pompe à Chaleur Air/Eau",
                Brand = "Daikin",
                Category = "Pompe à Chaleur",
                Price = 5500.00m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-3),
                WarrantyMonths = 36,
                SerialNumber = "DAI-2024-112233",
                ClientId = clientId2,
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            },
            new Article
            {
                Id = Guid.Parse("eeee5555-5555-5555-5555-555555555555"),
                Reference = "ART-005",
                Name = "WC Suspendu",
                Brand = "Grohe",
                Category = "Sanitaire",
                Price = 450.00m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-12),
                WarrantyMonths = 24,
                SerialNumber = "GRO-2024-445566",
                ClientId = clientId2,
                CreatedAt = DateTime.UtcNow.AddMonths(-12)
            },
            new Article
            {
                Id = Guid.Parse("eeee6666-6666-6666-6666-666666666666"),
                Reference = "ART-006",
                Name = "Chauffe-eau Électrique 100L",
                Brand = "Ariston",
                Category = "Eau Chaude Sanitaire",
                Price = 650.00m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-9),
                WarrantyMonths = 24,
                SerialNumber = "ARI-2024-778899",
                ClientId = clientId,
                CreatedAt = DateTime.UtcNow.AddMonths(-9)
            },
            new Article
            {
                Id = Guid.Parse("eeee7777-7777-7777-7777-777777777777"),
                Reference = "ART-007",
                Name = "Radiateur Design 1500W",
                Brand = "Acova",
                Category = "Radiateurs",
                Price = 180.00m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-24),
                WarrantyMonths = 12,
                SerialNumber = "ACO-2023-112233",
                ClientId = clientId,
                CreatedAt = DateTime.UtcNow.AddMonths(-24)
            },
            new Article
            {
                Id = Guid.Parse("eeee8888-8888-8888-8888-888888888888"),
                Reference = "ART-008",
                Name = "Chaudière Mixte Gaz",
                Brand = "De Dietrich",
                Category = "Chauffage Central",
                Price = 4200.00m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-15),
                WarrantyMonths = 24,
                SerialNumber = "DED-2023-445566",
                ClientId = clientId2,
                CreatedAt = DateTime.UtcNow.AddMonths(-15)
            },
            new Article
            {
                Id = Guid.Parse("eeee9999-9999-9999-9999-999999999999"),
                Reference = "ART-009",
                Name = "Robinetterie Évier",
                Brand = "Hansgrohe",
                Category = "Sanitaire",
                Price = 320.00m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-6),
                WarrantyMonths = 24,
                SerialNumber = "HAN-2024-667788",
                ClientId = clientId2,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            }
        };

            context.Articles.AddRange(seedArticles);
            await context.SaveChangesAsync();
        }
    }
}
