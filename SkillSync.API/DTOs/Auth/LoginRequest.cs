using System.ComponentModel.DataAnnotations;

namespace SkillSync.API.DTOs.Auth;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Senha { get; set; } = null!;
}

