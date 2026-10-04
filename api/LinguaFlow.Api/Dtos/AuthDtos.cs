using System.ComponentModel.DataAnnotations;

namespace LinguaFlow.Api.Dtos;

public class RegisterRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
    [MaxLength(100)] public string? DisplayName { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class AuthResponse
{
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}