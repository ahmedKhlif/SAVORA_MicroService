using System.ComponentModel.DataAnnotations;

namespace Savora.Shared.DTOs.Reclamations;

public class ClientDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public int ReclamationCount { get; set; }
    public int TotalReclamations { get; set; }
    public int ActiveReclamations { get; set; }
    public int TotalArticles { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateClientRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Full name is required")]
    [MinLength(2, ErrorMessage = "Full name must be at least 2 characters")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required")]
    [Phone(ErrorMessage = "Invalid phone format")]
    public string Phone { get; set; } = string.Empty;

    public string? Address { get; set; }
    public string? City { get; set; }
}

public class UpdateClientRequest
{
    [Required(ErrorMessage = "Full name is required")]
    [MinLength(2, ErrorMessage = "Full name must be at least 2 characters")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required")]
    [Phone(ErrorMessage = "Invalid phone format")]
    public string Phone { get; set; } = string.Empty;

    public string? Address { get; set; }
    public string? City { get; set; }
}

