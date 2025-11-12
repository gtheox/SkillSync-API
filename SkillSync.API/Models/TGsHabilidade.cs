using System.Collections.Generic;

namespace SkillSync.API.Models;

public partial class TGsHabilidade
{
    public decimal IdHabilidade { get; set; }

    public string NmHabilidade { get; set; } = null!;

    public virtual ICollection<TGsPerfilHabilidade> TGsPerfilHabilidades { get; set; } = new List<TGsPerfilHabilidade>();

    public virtual ICollection<TGsProjetoRequisito> TGsProjetoRequisitos { get; set; } = new List<TGsProjetoRequisito>();
}

