namespace SkillSync.API.DTOs.Usuario;

public class UsuarioResponseDto
{
    public decimal IdUsuario { get; set; }
    public string Nome { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public DateTime? DataCriacao { get; set; }
    public Dictionary<string, string>? Links { get; set; }
}

