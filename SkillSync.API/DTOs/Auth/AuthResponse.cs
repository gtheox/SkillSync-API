namespace SkillSync.API.DTOs.Auth;

public class AuthResponse
{
    public string? Token { get; set; }
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public decimal IdUsuario { get; set; }
}

