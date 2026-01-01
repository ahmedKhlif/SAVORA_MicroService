using Savora.ReclamationsService.Domain.Entities;
using Savora.Shared.Enums;

namespace Savora.ReclamationsService.Infrastructure.Data;

public static class ReclamationsDbSeeder
{
    public static async Task SeedAsync(ReclamationsDbContext context)
    {
        // Seed Clients
        if (!context.Clients.Any())
        {
            var clients = new List<Client>
            {
                new Client
                {
                    Id = Guid.Parse("cccc1111-1111-1111-1111-111111111111"),
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    FullName = "Ahmed Ben Ali",
                    Email = "client@savora.com",
                    Phone = "+216 71 123 456",
                    Address = "123 Avenue Habib Bourguiba",
                    City = "Tunis",
                    CreatedAt = DateTime.UtcNow
                },
                new Client
                {
                    Id = Guid.Parse("cccc2222-2222-2222-2222-222222222222"),
                    UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    FullName = "Fatima Trabelsi",
                    Email = "client2@savora.com",
                    Phone = "+216 71 789 012",
                    Address = "45 Rue de la Liberté",
                    City = "Sfax",
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Clients.AddRange(clients);
            await context.SaveChangesAsync();
        }

        // Seed Reclamations
        if (!context.Reclamations.Any())
        {
            var reclamations = new List<Reclamation>
            {
                new Reclamation
                {
                    Id = Guid.Parse("dddd1111-1111-1111-1111-111111111111"),
                    ClientId = Guid.Parse("cccc1111-1111-1111-1111-111111111111"),
                    ClientArticleId = Guid.Parse("eeee1111-1111-1111-1111-111111111111"), // Purchased article
                    Title = "Chaudière ne démarre plus",
                    Description = "Ma chaudière Viessmann ne démarre plus depuis ce matin. L'écran affiche un code erreur F28. J'ai essayé de la réinitialiser mais le problème persiste. Besoin d'une intervention urgente car nous sommes en plein hiver.",
                    Priority = Priority.High,
                    Status = ReclamationStatus.New,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Reclamation
                {
                    Id = Guid.Parse("dddd2222-2222-2222-2222-222222222222"),
                    ClientId = Guid.Parse("cccc1111-1111-1111-1111-111111111111"),
                    ClientArticleId = Guid.Parse("eeee2222-2222-2222-2222-222222222222"), // Purchased article
                    Title = "Fuite d'eau ballon",
                    Description = "Je constate une fuite d'eau au niveau du groupe de sécurité de mon ballon d'eau chaude Atlantic. La fuite est légère mais constante.",
                    Priority = Priority.Medium,
                    Status = ReclamationStatus.InProgress,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Reclamation
                {
                    Id = Guid.Parse("dddd3333-3333-3333-3333-333333333333"),
                    ClientId = Guid.Parse("cccc2222-2222-2222-2222-222222222222"),
                    ClientArticleId = Guid.Parse("eeee4444-4444-4444-4444-444444444444"), // Purchased article
                    Title = "Pompe à chaleur bruyante",
                    Description = "Ma pompe à chaleur Daikin fait un bruit anormal depuis quelques jours. Le bruit ressemble à un cliquetis métallique qui s'intensifie lors du démarrage.",
                    Priority = Priority.Low,
                    Status = ReclamationStatus.PendingIntervention,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            };

            foreach (var reclamation in reclamations)
            {
                reclamation.SetSlaDeadline();
            }

            context.Reclamations.AddRange(reclamations);
            await context.SaveChangesAsync();

            // Add history for each reclamation
            var histories = new List<ReclamationHistory>
            {
                new ReclamationHistory
                {
                    Id = Guid.NewGuid(),
                    ReclamationId = Guid.Parse("dddd1111-1111-1111-1111-111111111111"),
                    NewStatus = ReclamationStatus.New,
                    ActionType = "Created",
                    Comment = "Réclamation créée par le client",
                    ChangedBy = "client@savora.com",
                    ChangedAt = DateTime.UtcNow.AddDays(-2)
                },
                new ReclamationHistory
                {
                    Id = Guid.NewGuid(),
                    ReclamationId = Guid.Parse("dddd2222-2222-2222-2222-222222222222"),
                    NewStatus = ReclamationStatus.New,
                    ActionType = "Created",
                    Comment = "Réclamation créée par le client",
                    ChangedBy = "client@savora.com",
                    ChangedAt = DateTime.UtcNow.AddDays(-5)
                },
                new ReclamationHistory
                {
                    Id = Guid.NewGuid(),
                    ReclamationId = Guid.Parse("dddd2222-2222-2222-2222-222222222222"),
                    OldStatus = ReclamationStatus.New,
                    NewStatus = ReclamationStatus.InProgress,
                    ActionType = "StatusChange",
                    Comment = "Prise en charge de la réclamation",
                    ChangedBy = "admin@savora.com",
                    ChangedAt = DateTime.UtcNow.AddDays(-4)
                },
                new ReclamationHistory
                {
                    Id = Guid.NewGuid(),
                    ReclamationId = Guid.Parse("dddd3333-3333-3333-3333-333333333333"),
                    NewStatus = ReclamationStatus.New,
                    ActionType = "Created",
                    Comment = "Réclamation créée par le client",
                    ChangedBy = "client2@savora.com",
                    ChangedAt = DateTime.UtcNow.AddDays(-3)
                },
                new ReclamationHistory
                {
                    Id = Guid.NewGuid(),
                    ReclamationId = Guid.Parse("dddd3333-3333-3333-3333-333333333333"),
                    OldStatus = ReclamationStatus.New,
                    NewStatus = ReclamationStatus.PendingIntervention,
                    ActionType = "StatusChange",
                    Comment = "Intervention technique requise",
                    ChangedBy = "admin@savora.com",
                    ChangedAt = DateTime.UtcNow.AddDays(-2)
                }
            };

            context.ReclamationHistories.AddRange(histories);
            await context.SaveChangesAsync();
        }
    }
}

