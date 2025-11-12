using System.Text.Json.Serialization;

namespace SkillSync.API.DTOs.AI;

public class MatchResponse
{
    [JsonPropertyName("matches")]
    public List<MatchItem> Matches { get; set; } = new();
}

public class MatchItem
{
    [JsonPropertyName("id_perfil")]
    public int IdPerfil { get; set; } // API Python retorna int, não decimal
    
    [JsonPropertyName("score_compatibilidade")]
    public int ScoreCompatibilidade { get; set; }
    
    [JsonPropertyName("justificativa")]
    public string Justificativa { get; set; } = null!;
}

