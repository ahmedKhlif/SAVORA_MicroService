using Savora.InterventionsService.Domain.Entities;
using Savora.Shared.Enums;

namespace Savora.InterventionsService.Infrastructure.Data;

public static class InterventionsDbSeeder
{
    public static async Task SeedAsync(InterventionsDbContext context)
    {
        // Seed Technicians
        if (!context.Technicians.Any())
        {
            var technicians = new List<Technician>
            {
                new Technician
                {
                    Id = Guid.Parse("eeee1111-1111-1111-1111-111111111111"),
                    FullName = "Mohamed Khelifi",
                    Email = "m.khelifi@savora.com",
                    Phone = "+216 71 111 111",
                    Skills = new List<string> { "Chauffage", "Chaudière", "Pompe à chaleur" },
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Technician
                {
                    Id = Guid.Parse("eeee2222-2222-2222-2222-222222222222"),
                    FullName = "Sami Bouazizi",
                    Email = "s.bouazizi@savora.com",
                    Phone = "+216 71 222 222",
                    Skills = new List<string> { "Sanitaire", "Plomberie", "Eau chaude" },
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Technician
                {
                    Id = Guid.Parse("eeee3333-3333-3333-3333-333333333333"),
                    FullName = "Karim Hamdi",
                    Email = "k.hamdi@savora.com",
                    Phone = "+216 71 333 333",
                    Skills = new List<string> { "Climatisation", "Ventilation", "Pompe à chaleur" },
                    IsAvailable = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Technicians.AddRange(technicians);
            await context.SaveChangesAsync();
        }

        // Seed sample intervention
        if (!context.Interventions.Any())
        {
            var intervention = new Intervention
            {
                Id = Guid.Parse("ffff1111-1111-1111-1111-111111111111"),
                ReclamationId = Guid.Parse("dddd2222-2222-2222-2222-222222222222"),
                TechnicianId = Guid.Parse("eeee2222-2222-2222-2222-222222222222"),
                Status = InterventionStatus.Planned,
                PlannedDate = DateTime.UtcNow.AddDays(2),
                Notes = "Intervention planifiée pour fuite d'eau sur ballon",
                IsFree = true, // Under warranty
                CreatedAt = DateTime.UtcNow
            };

            context.Interventions.Add(intervention);
            await context.SaveChangesAsync();
        }
    }
}

