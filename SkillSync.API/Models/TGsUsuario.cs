using System;
using System.Collections.Generic;

namespace SkillSync.API.Models;

public partial class TGsUsuario
{
    public decimal IdUsuario { get; set; }

    public string NmUsuario { get; set; } = null!;

    public string DsEmail { get; set; } = null!;

    public string DsSenha { get; set; } = null!;

    public string FlRole { get; set; } = null!;

    public DateTime? DtCriacao { get; set; }

    public virtual ICollection<TGsDicasIa> TGsDicasIas { get; set; } = new List<TGsDicasIa>();

    public virtual TGsPerfisFreelancer? TGsPerfisFreelancer { get; set; }

    public virtual ICollection<TGsProjetosContratante> TGsProjetosContratantes { get; set; } = new List<TGsProjetosContratante>();
}

