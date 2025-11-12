namespace SkillSync.API.DTOs.Dica;

public class DicaResponseDto
{
    public decimal IdDica { get; set; }
    public string Titulo { get; set; } = null!;
    public string Conteudo { get; set; } = null!;
    public DateTime? DataGeracao { get; set; }
    public decimal IdAdminGerador { get; set; }
}

