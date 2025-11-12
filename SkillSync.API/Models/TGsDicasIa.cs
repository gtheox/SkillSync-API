using System;

namespace SkillSync.API.Models;

public partial class TGsDicasIa
{
    public decimal IdDica { get; set; }

    public decimal IdAdminGerador { get; set; }

    public string DsTitulo { get; set; } = null!;

    public string DsConteudo { get; set; } = null!;

    public DateTime? DtGeracao { get; set; }

    public virtual TGsUsuario IdAdminGeradorNavigation { get; set; } = null!;
}

