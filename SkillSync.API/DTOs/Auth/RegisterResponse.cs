namespace SkillSync.API.DTOs.Auth;

public class RegisterResponse
{
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public decimal IdUsuario { get; set; }
    public string Message { get; set; } = "Usuário registrado com sucesso. Faça login para obter o token.";
}
