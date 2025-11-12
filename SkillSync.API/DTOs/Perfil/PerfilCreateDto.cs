using System.ComponentModel.DataAnnotations;

namespace SkillSync.API.DTOs.Perfil;

public class PerfilCreateDto
{
    [StringLength(150)]
    public string? TituloProfissional { get; set; }

    public string? Resumo { get; set; }

    public decimal? ValorHora { get; set; }

    public List<decimal>? Habilidades { get; set; }
}

