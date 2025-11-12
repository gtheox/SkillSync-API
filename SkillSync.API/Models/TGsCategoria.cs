using System.Collections.Generic;

namespace SkillSync.API.Models;

public partial class TGsCategoria
{
    public decimal IdCategoria { get; set; }

    public string NmCategoria { get; set; } = null!;

    public virtual ICollection<TGsProjetosContratante> TGsProjetosContratantes { get; set; } = new List<TGsProjetosContratante>();
}

