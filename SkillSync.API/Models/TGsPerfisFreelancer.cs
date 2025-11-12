using System;
using System.Collections.Generic;

namespace SkillSync.API.Models;

public partial class TGsPerfisFreelancer
{
    public decimal IdPerfil { get; set; }

    public decimal IdUsuario { get; set; }

    public string? DsTituloProfissional { get; set; }

    public string? DsResumo { get; set; }

    public decimal? VlHora { get; set; }

    public DateTime? DtUltimaAtualizacao { get; set; }

    public virtual TGsUsuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<TGsPerfilHabilidade> TGsPerfilHabilidades { get; set; } = new List<TGsPerfilHabilidade>();
}

