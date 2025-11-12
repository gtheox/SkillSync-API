using Microsoft.EntityFrameworkCore;
using SkillSync.API.Models;

namespace SkillSync.API.Data;

public partial class SkillSyncDbContext : DbContext
{
    public SkillSyncDbContext()
    {
    }

    public SkillSyncDbContext(DbContextOptions<SkillSyncDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TGsUsuario> TGsUsuarios { get; set; }

    public virtual DbSet<TGsPerfisFreelancer> TGsPerfisFreelancers { get; set; }

    public virtual DbSet<TGsProjetosContratante> TGsProjetosContratantes { get; set; }

    public virtual DbSet<TGsDicasIa> TGsDicasIas { get; set; }

    public virtual DbSet<TGsCategoria> TGsCategorias { get; set; }

    public virtual DbSet<TGsHabilidade> TGsHabilidades { get; set; }

    public virtual DbSet<TGsPerfilHabilidade> TGsPerfilHabilidades { get; set; }

    public virtual DbSet<TGsProjetoRequisito> TGsProjetoRequisitos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Não definir schema padrão - usar o schema do usuário conectado (RM555962)
        // As tabelas são criadas no schema do usuário que executa o script SQL
        // modelBuilder.HasDefaultSchema("SYSTEM"); // Removido - tabelas estão no schema do usuário

        modelBuilder.Entity<TGsUsuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PK_GS_USUARIOS");

            entity.ToTable("T_GS_USUARIOS");

            entity.HasIndex(e => e.DsEmail, "UK_GS_USUARIOS_EMAIL").IsUnique();

            entity.Property(e => e.IdUsuario)
                .HasColumnType("NUMBER(10)")
                .HasColumnName("ID_USUARIO");

            entity.Property(e => e.NmUsuario)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("NM_USUARIO");

            entity.Property(e => e.DsEmail)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("DS_EMAIL");

            entity.Property(e => e.DsSenha)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("DS_SENHA");

            entity.Property(e => e.FlRole)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("FL_ROLE");

            entity.Property(e => e.DtCriacao)
                .HasColumnType("DATE")
                .HasColumnName("DT_CRIACAO");
        });

        modelBuilder.Entity<TGsPerfisFreelancer>(entity =>
        {
            entity.HasKey(e => e.IdPerfil).HasName("PK_GS_PERFIS_FREELANCER");

            entity.ToTable("T_GS_PERFIS_FREELANCER");

            entity.HasIndex(e => e.IdUsuario, "UK_GS_PERFIS_USUARIO").IsUnique();

            // Configurar ID_PERFIL para usar a sequence do Oracle
            entity.Property(e => e.IdPerfil)
                .HasColumnType("NUMBER(10)")
                .HasColumnName("ID_PERFIL")
                .ValueGeneratedOnAdd(); // Oracle gera o ID automaticamente via sequence

            entity.Property(e => e.IdUsuario)
                .HasColumnType("NUMBER(10)")
                .HasColumnName("ID_USUARIO");

            entity.Property(e => e.DsTituloProfissional)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("DS_TITULO_PROFISSIONAL");

            entity.Property(e => e.DsResumo)
                .HasColumnType("CLOB")
                .HasColumnName("DS_RESUMO");

            entity.Property(e => e.VlHora)
                .HasColumnType("NUMBER(10,2)")
                .HasColumnName("VL_HORA");

            entity.Property(e => e.DtUltimaAtualizacao)
                .HasColumnType("DATE")
                .HasColumnName("DT_ULTIMA_ATUALIZACAO");

            entity.HasOne(d => d.IdUsuarioNavigation)
                .WithOne(p => p.TGsPerfisFreelancer)
                .HasForeignKey<TGsPerfisFreelancer>(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GS_PERFIS_USUARIO");
        });

        modelBuilder.Entity<TGsProjetosContratante>(entity =>
        {
            entity.HasKey(e => e.IdProjeto).HasName("PK_GS_PROJETOS");

            entity.ToTable("T_GS_PROJETOS_CONTRATANTE");

            // Configurar ID_PROJETO para usar a sequence do Oracle
            // O Oracle vai gerar automaticamente via DEFAULT SQ_GS_PROJETOS_CONTRATANTE.NEXTVAL
            entity.Property(e => e.IdProjeto)
                .HasColumnType("NUMBER(10)")
                .HasColumnName("ID_PROJETO")
                .ValueGeneratedOnAdd(); // Não definir o ID manualmente, deixar o Oracle gerar

            entity.Property(e => e.IdUsuarioContratante)
                .HasColumnType("NUMBER(10)")
                .HasColumnName("ID_USUARIO_CONTRATANTE");

            entity.Property(e => e.IdCategoria)
                .HasColumnType("NUMBER(10)")
                .HasColumnName("ID_CATEGORIA");

            entity.Property(e => e.DsTitulo)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("DS_TITULO");

            entity.Property(e => e.DsDescricao)
                .HasColumnType("CLOB")
                .HasColumnName("DS_DESCRICAO");

            entity.Property(e => e.VlOrcamento)
                .HasColumnType("NUMBER(10,2)")
                .HasColumnName("VL_ORCAMENTO");

            entity.Property(e => e.StProjeto)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("ST_PROJETO")
                .HasDefaultValueSql("'ABERTO'");

            entity.Property(e => e.DtPublicacao)
                .HasColumnType("DATE")
                .HasColumnName("DT_PUBLICACAO");

            entity.HasOne(d => d.IdCategoriaNavigation)
                .WithMany(p => p.TGsProjetosContratantes)
                .HasForeignKey(d => d.IdCategoria)
                .HasConstraintName("FK_GS_PROJETOS_CATEGORIA");

            entity.HasOne(d => d.IdUsuarioContratanteNavigation)
                .WithMany(p => p.TGsProjetosContratantes)
                .HasForeignKey(d => d.IdUsuarioContratante)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GS_PROJETOS_USUARIO");
        });

        modelBuilder.Entity<TGsDicasIa>(entity =>
        {
            entity.HasKey(e => e.IdDica).HasName("PK_GS_DICAS_IA");

            entity.ToTable("T_GS_DICAS_IA");

            entity.Property(e => e.IdDica)
                .HasColumnType("NUMBER(10)")
                .HasColumnName("ID_DICA");

            entity.Property(e => e.IdAdminGerador)
                .HasColumnType("NUMBER(10)")
                .HasColumnName("ID_ADMIN_GERADOR");

            entity.Property(e => e.DsTitulo)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("DS_TITULO");

            entity.Property(e => e.DsConteudo)
                .HasColumnType("CLOB")
                .HasColumnName("DS_CONTEUDO");

            entity.Property(e => e.DtGeracao)
                .HasColumnType("DATE")
                .HasColumnName("DT_GERACAO");

            entity.HasOne(d => d.IdAdminGeradorNavigation)
                .WithMany(p => p.TGsDicasIas)
                .HasForeignKey(d => d.IdAdminGerador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GS_DICAS_ADMIN");
        });

        modelBuilder.Entity<TGsCategoria>(entity =>
        {
            entity.HasKey(e => e.IdCategoria).HasName("PK_GS_CATEGORIAS");

            entity.ToTable("T_GS_CATEGORIAS");

            entity.HasIndex(e => e.NmCategoria, "UK_GS_CATEGORIAS_NOME").IsUnique();

            entity.Property(e => e.IdCategoria)
                .HasColumnType("NUMBER(10)")
                .HasColumnName("ID_CATEGORIA");

            entity.Property(e => e.NmCategoria)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NM_CATEGORIA");
        });

        modelBuilder.Entity<TGsHabilidade>(entity =>
        {
            entity.HasKey(e => e.IdHabilidade).HasName("PK_GS_HABILIDADES");

            entity.ToTable("T_GS_HABILIDADES");

            entity.HasIndex(e => e.NmHabilidade, "UK_GS_HABILIDADES_NOME").IsUnique();

            entity.Property(e => e.IdHabilidade)
                .HasColumnType("NUMBER(10)")
                .HasColumnName("ID_HABILIDADE");

            entity.Property(e => e.NmHabilidade)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NM_HABILIDADE");
        });

        modelBuilder.Entity<TGsPerfilHabilidade>(entity =>
        {
            entity.HasKey(e => new { e.IdPerfil, e.IdHabilidade }).HasName("PK_GS_PERFIL_HABILIDADES");

            entity.ToTable("T_GS_PERFIL_HABILIDADES");

            entity.Property(e => e.IdPerfil)
                .HasColumnType("NUMBER(10)")
                .HasColumnName("ID_PERFIL");

            entity.Property(e => e.IdHabilidade)
                .HasColumnType("NUMBER(10)")
                .HasColumnName("ID_HABILIDADE");

            entity.HasOne(d => d.IdHabilidadeNavigation)
                .WithMany(p => p.TGsPerfilHabilidades)
                .HasForeignKey(d => d.IdHabilidade)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GS_PERFILHAB_HABILIDADE");

            entity.HasOne(d => d.IdPerfilNavigation)
                .WithMany(p => p.TGsPerfilHabilidades)
                .HasForeignKey(d => d.IdPerfil)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GS_PERFILHAB_PERFIL");
        });

        modelBuilder.Entity<TGsProjetoRequisito>(entity =>
        {
            entity.HasKey(e => new { e.IdProjeto, e.IdHabilidade }).HasName("PK_GS_PROJETO_REQUISITOS");

            entity.ToTable("T_GS_PROJETO_REQUISITOS");

            entity.Property(e => e.IdProjeto)
                .HasColumnType("NUMBER(10)")
                .HasColumnName("ID_PROJETO");

            entity.Property(e => e.IdHabilidade)
                .HasColumnType("NUMBER(10)")
                .HasColumnName("ID_HABILIDADE");

            entity.HasOne(d => d.IdHabilidadeNavigation)
                .WithMany(p => p.TGsProjetoRequisitos)
                .HasForeignKey(d => d.IdHabilidade)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GS_PROJREQ_HABILIDADE");

            entity.HasOne(d => d.IdProjetoNavigation)
                .WithMany(p => p.TGsProjetoRequisitos)
                .HasForeignKey(d => d.IdProjeto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GS_PROJREQ_PROJETO");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

