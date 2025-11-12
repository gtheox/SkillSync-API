namespace SkillSync.API.DTOs.Projeto;

public class ProjetoResponseDto
{
    public decimal IdProjeto { get; set; }
    public string Titulo { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public decimal? IdCategoria { get; set; }
    public string? CategoriaNome { get; set; }
    public decimal? Orcamento { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? DataPublicacao { get; set; }
    public decimal IdUsuarioContratante { get; set; }
    public List<string>? HabilidadesRequisitadas { get; set; }
    public Dictionary<string, string>? Links { get; set; }
}

