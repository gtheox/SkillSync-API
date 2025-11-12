using System.ComponentModel.DataAnnotations;

namespace SkillSync.API.DTOs.Projeto;

public class ProjetoUpdateDto
{
    [StringLength(150)]
    public string? Titulo { get; set; }

    public string? Descricao { get; set; }

    public decimal? IdCategoria { get; set; }

    public decimal? Orcamento { get; set; }

    public string? Status { get; set; }
}

