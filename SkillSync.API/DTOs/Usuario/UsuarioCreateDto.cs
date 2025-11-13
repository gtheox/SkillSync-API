using System.ComponentModel.DataAnnotations;

namespace SkillSync.API.DTOs.Usuario;

public class UsuarioCreateDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 100 caracteres")]
    public string Nome { get; set; } = null!;

    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [StringLength(100, ErrorMessage = "Email deve ter no máximo 100 caracteres")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Senha é obrigatória")]
    [StringLength(200, MinimumLength = 6, ErrorMessage = "Senha deve ter entre 6 e 200 caracteres")]
    public string Senha { get; set; } = null!;

    [Required(ErrorMessage = "Role é obrigatório")]
    [RegularExpression("^(FREELANCER|CONTRATANTE|ADMIN)$", ErrorMessage = "Role deve ser FREELANCER, CONTRATANTE ou ADMIN")]
    public string Role { get; set; } = null!;
}

