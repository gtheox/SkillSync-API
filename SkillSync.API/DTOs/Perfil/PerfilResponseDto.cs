namespace SkillSync.API.DTOs.Perfil;

public class PerfilResponseDto
{
    public decimal IdPerfil { get; set; }
    public decimal IdUsuario { get; set; }
    public string? TituloProfissional { get; set; }
    public string? Resumo { get; set; }
    public decimal? ValorHora { get; set; }
    public DateTime? DataUltimaAtualizacao { get; set; }
    public List<string>? Habilidades { get; set; }
    public Dictionary<string, string>? Links { get; set; }
}

