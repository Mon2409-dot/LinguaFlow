using Microsoft.AspNetCore.Identity;

namespace LinguaFlow.Api.Models;

public class AppUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}