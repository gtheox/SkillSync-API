using System;
using System.Collections.Generic;

namespace SkillSync.API.Models;

public partial class TGsProjetosContratante
{
    public decimal IdProjeto { get; set; }

    public decimal IdUsuarioContratante { get; set; }

    public decimal? IdCategoria { get; set; }

    public string DsTitulo { get; set; } = null!;

    public string DsDescricao { get; set; } = null!;

    public decimal? VlOrcamento { get; set; }

    public string StProjeto { get; set; } = null!;

    public DateTime? DtPublicacao { get; set; }

    public virtual TGsCategoria? IdCategoriaNavigation { get; set; }

    public virtual TGsUsuario IdUsuarioContratanteNavigation { get; set; } = null!;

    public virtual ICollection<TGsProjetoRequisito> TGsProjetoRequisitos { get; set; } = new List<TGsProjetoRequisito>();
}

