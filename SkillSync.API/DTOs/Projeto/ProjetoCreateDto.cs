using System.ComponentModel.DataAnnotations;

namespace SkillSync.API.DTOs.Projeto;

public class ProjetoCreateDto
{
    [Required]
    [StringLength(150)]
    public string Titulo { get; set; } = null!;

    [Required]
    public string Descricao { get; set; } = null!;

    public decimal? IdCategoria { get; set; }

    public decimal? Orcamento { get; set; }

    public List<decimal>? HabilidadesRequisitadas { get; set; }
}

