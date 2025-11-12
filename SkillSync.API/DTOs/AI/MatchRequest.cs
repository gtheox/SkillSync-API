using System.Text.Json.Serialization;

namespace SkillSync.API.DTOs.AI;

public class MatchRequest
{
    [JsonPropertyName("projeto")]
    public ProjetoDto Projeto { get; set; } = null!;
    
    [JsonPropertyName("perfis")]
    public List<PerfilDto> Perfis { get; set; } = new();
}

public class ProjetoDto
{
    [JsonPropertyName("titulo")]
    public string Titulo { get; set; } = null!;
    
    [JsonPropertyName("descricao")]
    public string Descricao { get; set; } = null!;
}

public class PerfilDto
{
    [JsonPropertyName("id_perfil")]
    public int IdPerfil { get; set; } // API Python espera int, não decimal
    
    [JsonPropertyName("titulo_profissional")]
    public string TituloProfissional { get; set; } = null!;
    
    [JsonPropertyName("resumo")]
    public string Resumo { get; set; } = null!;
    
    [JsonPropertyName("habilidades")]
    public List<string> Habilidades { get; set; } = new();
}

