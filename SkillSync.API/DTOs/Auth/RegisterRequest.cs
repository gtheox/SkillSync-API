using System.ComponentModel.DataAnnotations;

namespace SkillSync.API.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [StringLength(100)]
    public string Nome { get; set; } = null!;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(200, MinimumLength = 6)]
    public string Senha { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string Role { get; set; } = null!; // FREELANCER, CONTRATANTE, ADMIN
}

