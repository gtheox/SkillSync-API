namespace SkillSync.API.Models;

public partial class TGsProjetoRequisito
{
    public decimal IdProjeto { get; set; }

    public decimal IdHabilidade { get; set; }

    public virtual TGsHabilidade IdHabilidadeNavigation { get; set; } = null!;

    public virtual TGsProjetosContratante IdProjetoNavigation { get; set; } = null!;
}

