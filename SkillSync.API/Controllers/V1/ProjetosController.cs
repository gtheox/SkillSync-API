using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using SkillSync.API.Data;
using SkillSync.API.DTOs.Projeto;
using SkillSync.API.Models;
using SkillSync.API.Services;
using System.Security.Claims;

namespace SkillSync.API.Controllers.V1;

[ApiController]
[Route("api/v{version:apiVersion}/projetos")]
[Authorize]
[Produces("application/json")]
[ApiVersion("1.0")]
public class ProjetosController : ControllerBase
{
    private readonly SkillSyncDbContext _context;
    private readonly IMLService _mlService;
    private readonly ILogger<ProjetosController> _logger;

    public ProjetosController(
        SkillSyncDbContext context,
        IMLService mlService,
        ILogger<ProjetosController> logger)
    {
        _context = context;
        _mlService = mlService;
        _logger = logger;
    }

    /// <summary>
    /// Lista projetos com paginação
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProjetoResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ProjetoResponseDto>>> GetProjetos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var totalCount = await _context.TGsProjetosContratantes.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var projetos = await _context.TGsProjetosContratantes
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.TGsProjetoRequisitos)
                    .ThenInclude(pr => pr.IdHabilidadeNavigation)
                .OrderByDescending(p => p.DtPublicacao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var projetosDto = projetos.Select(p => MapToDto(p, Request)).ToList();

            var response = new PagedResponse<ProjetoResponseDto>
            {
                Data = projetosDto,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Links = GeneratePaginationLinks(page, totalPages)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar projetos");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Busca projeto por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProjetoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjetoResponseDto>> GetProjeto(decimal id)
    {
        try
        {
            var projeto = await _context.TGsProjetosContratantes
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.TGsProjetoRequisitos)
                    .ThenInclude(pr => pr.IdHabilidadeNavigation)
                .FirstOrDefaultAsync(p => p.IdProjeto == id);

            if (projeto == null)
            {
                return NotFound(new { message = "Projeto não encontrado" });
            }

            return Ok(MapToDto(projeto, Request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar projeto {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Cria um novo projeto
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProjetoResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjetoResponseDto>> CreateProjeto([FromBody] ProjetoCreateDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();

            // Usar ML.NET para prever categoria se não fornecida
            decimal? categoriaId = dto.IdCategoria;
            if (categoriaId == null)
            {
                var categoriaPrevista = await _mlService.PreverCategoriaAsync(dto.Titulo, dto.Descricao);
                if (categoriaPrevista != null)
                {
                    categoriaId = categoriaPrevista;
                    _logger.LogInformation("Categoria prevista pelo ML.NET: {CategoriaId}", categoriaId);
                }
            }

            // Criar projeto usando stored procedure do Oracle
            // A procedure não retorna o ID, então usamos SQL raw para inserir e obter o ID
            // Usar stored procedure do Oracle que já existe
            await _context.Database.ExecuteSqlRawAsync(
                "BEGIN PKG_GERENCIAMENTO.SP_CRIAR_PROJETO(:p_userId, :p_categoriaId, :p_titulo, :p_descricao, :p_orcamento); END;",
                new Oracle.ManagedDataAccess.Client.OracleParameter("p_userId", userId),
                new Oracle.ManagedDataAccess.Client.OracleParameter("p_categoriaId", (object?)categoriaId ?? DBNull.Value),
                new Oracle.ManagedDataAccess.Client.OracleParameter("p_titulo", dto.Titulo),
                new Oracle.ManagedDataAccess.Client.OracleParameter("p_descricao", dto.Descricao),
                new Oracle.ManagedDataAccess.Client.OracleParameter("p_orcamento", (object?)dto.Orcamento ?? DBNull.Value));
            
            // Buscar o projeto recém-criado usando título, usuário e data
            // Como a procedure não retorna o ID, buscamos o projeto mais recente deste usuário com este título
            var projeto = await _context.TGsProjetosContratantes
                .Where(p => p.IdUsuarioContratante == userId 
                    && p.DsTitulo == dto.Titulo)
                .OrderByDescending(p => p.IdProjeto)
                .FirstOrDefaultAsync();
            
            if (projeto == null)
            {
                _logger.LogError("Falha ao criar projeto - não foi possível encontrar o projeto criado após a procedure");
                return StatusCode(500, new { message = "Erro ao criar projeto. Tente novamente." });
            }
            
            var idProjeto = projeto.IdProjeto;
            _logger.LogInformation("Projeto criado com ID: {IdProjeto} pelo usuário {UserId}", idProjeto, userId);

            // Adicionar habilidades requisitadas usando stored procedure
            if (dto.HabilidadesRequisitadas != null && dto.HabilidadesRequisitadas.Any())
            {
                foreach (var habilidadeId in dto.HabilidadesRequisitadas)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "BEGIN PKG_GERENCIAMENTO.SP_ADICIONAR_REQUISITO_PROJETO(:p_idProjeto, :p_idHabilidade); END;",
                        new Oracle.ManagedDataAccess.Client.OracleParameter("p_idProjeto", idProjeto),
                        new Oracle.ManagedDataAccess.Client.OracleParameter("p_idHabilidade", habilidadeId));
                }
            }

            // Buscar projeto completo para retornar
            var projetoCompleto = await _context.TGsProjetosContratantes
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.TGsProjetoRequisitos)
                    .ThenInclude(pr => pr.IdHabilidadeNavigation)
                .FirstOrDefaultAsync(p => p.IdProjeto == idProjeto);

            return CreatedAtAction(nameof(GetProjeto), new { id = idProjeto }, 
                MapToDto(projetoCompleto!, Request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar projeto");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Atualiza um projeto existente
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProjetoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjetoResponseDto>> UpdateProjeto(decimal id, [FromBody] ProjetoUpdateDto dto)
    {
        try
        {
            var projeto = await _context.TGsProjetosContratantes
                .FirstOrDefaultAsync(p => p.IdProjeto == id);

            if (projeto == null)
            {
                return NotFound(new { message = "Projeto não encontrado" });
            }

            // Verificar permissões: admin pode editar qualquer projeto, contratante apenas o seu
            var userId = GetUserId();
            var isAdmin = IsAdmin();
            if (!isAdmin && projeto.IdUsuarioContratante != userId)
            {
                return Forbid();
            }

            if (dto.Titulo != null) projeto.DsTitulo = dto.Titulo;
            if (dto.Descricao != null) projeto.DsDescricao = dto.Descricao;
            if (dto.IdCategoria != null) projeto.IdCategoria = dto.IdCategoria;
            if (dto.Orcamento != null) projeto.VlOrcamento = dto.Orcamento;
            if (dto.Status != null) projeto.StProjeto = dto.Status;

            await _context.SaveChangesAsync();

            var projetoCompleto = await _context.TGsProjetosContratantes
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.TGsProjetoRequisitos)
                    .ThenInclude(pr => pr.IdHabilidadeNavigation)
                .FirstOrDefaultAsync(p => p.IdProjeto == id);

            return Ok(MapToDto(projetoCompleto!, Request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar projeto {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Deleta um projeto
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteProjeto(decimal id)
    {
        try
        {
            var projeto = await _context.TGsProjetosContratantes
                .FirstOrDefaultAsync(p => p.IdProjeto == id);

            if (projeto == null)
            {
                return NotFound(new { message = "Projeto não encontrado" });
            }

            // Verificar permissões: admin pode editar qualquer projeto, contratante apenas o seu
            var userId = GetUserId();
            var isAdmin = IsAdmin();
            if (!isAdmin && projeto.IdUsuarioContratante != userId)
            {
                return Forbid();
            }

            // Deletar registros filhos primeiro (requisitos do projeto)
            var requisitosProjeto = await _context.TGsProjetoRequisitos
                .Where(pr => pr.IdProjeto == id)
                .ToListAsync();
            
            if (requisitosProjeto.Any())
            {
                _context.TGsProjetoRequisitos.RemoveRange(requisitosProjeto);
                await _context.SaveChangesAsync();
            }

            // Agora pode deletar o projeto
            _context.TGsProjetosContratantes.Remove(projeto);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar projeto {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    private decimal GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (decimal.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("Usuário não autenticado");
    }

    private bool IsAdmin()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        return roleClaim == "ADMIN";
    }

    private static ProjetoResponseDto MapToDto(TGsProjetosContratante projeto, HttpRequest request)
    {
        var baseUrl = $"{request.Scheme}://{request.Host}";
        var dto = new ProjetoResponseDto
        {
            IdProjeto = projeto.IdProjeto,
            Titulo = projeto.DsTitulo,
            Descricao = projeto.DsDescricao,
            IdCategoria = projeto.IdCategoria,
            CategoriaNome = projeto.IdCategoriaNavigation?.NmCategoria,
            Orcamento = projeto.VlOrcamento,
            Status = projeto.StProjeto,
            DataPublicacao = projeto.DtPublicacao,
            IdUsuarioContratante = projeto.IdUsuarioContratante,
            HabilidadesRequisitadas = projeto.TGsProjetoRequisitos
                .Select(pr => pr.IdHabilidadeNavigation.NmHabilidade)
                .ToList(),
            Links = new Dictionary<string, string>
            {
                { "self", $"{baseUrl}/api/v1.0/projetos/{projeto.IdProjeto}" },
                { "edit", $"{baseUrl}/api/v1.0/projetos/{projeto.IdProjeto}" },
                { "delete", $"{baseUrl}/api/v1.0/projetos/{projeto.IdProjeto}" },
                { "find_matches", $"{baseUrl}/api/v1.0/projetos/{projeto.IdProjeto}/gerar-matches" },
                { "v2", $"{baseUrl}/api/v2.0/projetos/{projeto.IdProjeto}" }
            }
        };
        return dto;
    }

    private static Dictionary<string, string> GeneratePaginationLinks(int currentPage, int totalPages)
    {
        var baseUrl = "/api/v1.0/projetos";
        var links = new Dictionary<string, string>
        {
            { "self", $"{baseUrl}?page={currentPage}" }
        };

        if (currentPage > 1)
        {
            links.Add("prev", $"{baseUrl}?page={currentPage - 1}");
        }

        if (currentPage < totalPages)
        {
            links.Add("next", $"{baseUrl}?page={currentPage + 1}");
        }

        links.Add("first", $"{baseUrl}?page=1");
        links.Add("last", $"{baseUrl}?page={totalPages}");

        return links;
    }
}

