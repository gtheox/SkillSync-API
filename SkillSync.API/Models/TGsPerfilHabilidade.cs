namespace SkillSync.API.Models;

public partial class TGsPerfilHabilidade
{
    public decimal IdPerfil { get; set; }

    public decimal IdHabilidade { get; set; }

    public virtual TGsHabilidade IdHabilidadeNavigation { get; set; } = null!;

    public virtual TGsPerfisFreelancer IdPerfilNavigation { get; set; } = null!;
}

