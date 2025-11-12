using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSync.API.Data;
using SkillSync.API.DTOs.Projeto;
using SkillSync.API.Models;
using SkillSync.API.Services;
using System.Security.Claims;

namespace SkillSync.API.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/projetos")]
[Authorize]
[Produces("application/json")]
[ApiVersion("2.0")]
public class ProjetosV2Controller : ControllerBase
{
    private readonly SkillSyncDbContext _context;
    private readonly IMLService _mlService;
    private readonly ILogger<ProjetosV2Controller> _logger;

    public ProjetosV2Controller(
        SkillSyncDbContext context,
        IMLService mlService,
        ILogger<ProjetosV2Controller> logger)
    {
        _context = context;
        _mlService = mlService;
        _logger = logger;
    }

    /// <summary>
    /// Lista projetos com paginação (V2 - Melhorado com filtros)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProjetoResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ProjetoResponseDto>>> GetProjetos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] decimal? categoriaId = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = _context.TGsProjetosContratantes.AsQueryable();

            // Filtros V2
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.StProjeto == status.ToUpper());
            }

            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.IdCategoria == categoriaId.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var projetos = await query
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
                Links = GeneratePaginationLinks(page, totalPages, status, categoriaId)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar projetos V2");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Busca projeto por ID (V2 - Melhorado com mais informações)
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
                .Include(p => p.IdUsuarioContratanteNavigation)
                .Include(p => p.TGsProjetoRequisitos)
                    .ThenInclude(pr => pr.IdHabilidadeNavigation)
                .FirstOrDefaultAsync(p => p.IdProjeto == id);

            if (projeto == null)
            {
                return NotFound(new { message = "Projeto não encontrado" });
            }

            var dto = MapToDto(projeto, Request);
            
            // V2: Adicionar informações adicionais
            dto.Links!["version"] = "v2";
            dto.Links!["api_version"] = "2.0";

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar projeto V2 {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Cria um novo projeto (V2 - Com sugestão automática de categoria via ML.NET)
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

            // V2: Sempre usar ML.NET para sugerir categoria, mesmo se fornecida
            decimal? categoriaId = dto.IdCategoria;
            var categoriaPrevista = await _mlService.PreverCategoriaAsync(dto.Titulo, dto.Descricao);
            
            if (categoriaPrevista != null)
            {
                _logger.LogInformation("V2: Categoria prevista pelo ML.NET: {CategoriaId} (fornecida: {Fornecida})", 
                    categoriaPrevista, categoriaId);
                
                // V2: Se não foi fornecida categoria, usar a prevista
                if (categoriaId == null)
                {
                    categoriaId = categoriaPrevista;
                }
            }

            var projeto = new TGsProjetosContratante
            {
                IdUsuarioContratante = userId,
                IdCategoria = categoriaId,
                DsTitulo = dto.Titulo,
                DsDescricao = dto.Descricao,
                VlOrcamento = dto.Orcamento,
                StProjeto = "ABERTO",
                DtPublicacao = DateTime.Now
            };

            _context.TGsProjetosContratantes.Add(projeto);
            await _context.SaveChangesAsync();

            if (dto.HabilidadesRequisitadas != null && dto.HabilidadesRequisitadas.Any())
            {
                foreach (var habilidadeId in dto.HabilidadesRequisitadas)
                {
                    var requisito = new TGsProjetoRequisito
                    {
                        IdProjeto = projeto.IdProjeto,
                        IdHabilidade = habilidadeId
                    };
                    _context.TGsProjetoRequisitos.Add(requisito);
                }
                await _context.SaveChangesAsync();
            }

            var projetoCompleto = await _context.TGsProjetosContratantes
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.TGsProjetoRequisitos)
                    .ThenInclude(pr => pr.IdHabilidadeNavigation)
                .FirstOrDefaultAsync(p => p.IdProjeto == projeto.IdProjeto);

            var responseDto = MapToDto(projetoCompleto!, Request);
            responseDto.Links!["version"] = "v2";
            responseDto.Links!["api_version"] = "2.0";
            if (categoriaPrevista != null && categoriaId == categoriaPrevista)
            {
                responseDto.Links!["ml_suggestion"] = "true";
            }

            return CreatedAtAction(nameof(GetProjeto), new { id = projeto.IdProjeto }, responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar projeto V2");
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
                { "self", $"{baseUrl}/api/v2.0/projetos/{projeto.IdProjeto}" },
                { "edit", $"{baseUrl}/api/v2.0/projetos/{projeto.IdProjeto}" },
                { "delete", $"{baseUrl}/api/v2.0/projetos/{projeto.IdProjeto}" },
                { "find_matches", $"{baseUrl}/api/v2.0/projetos/{projeto.IdProjeto}/gerar-matches" },
                { "v1", $"{baseUrl}/api/v1.0/projetos/{projeto.IdProjeto}" }
            }
        };
        return dto;
    }

    private static Dictionary<string, string> GeneratePaginationLinks(
        int currentPage, 
        int totalPages, 
        string? status = null, 
        decimal? categoriaId = null)
    {
        var baseUrl = "/api/v2.0/projetos";
        var queryParams = new List<string> { $"page={currentPage}" };
        
        if (!string.IsNullOrEmpty(status))
        {
            queryParams.Add($"status={status}");
        }
        
        if (categoriaId.HasValue)
        {
            queryParams.Add($"categoriaId={categoriaId.Value}");
        }
        
        var queryString = string.Join("&", queryParams);
        var links = new Dictionary<string, string>
        {
            { "self", $"{baseUrl}?{queryString}" }
        };

        if (currentPage > 1)
        {
            var prevQuery = queryParams.Where(p => !p.StartsWith("page=")).ToList();
            prevQuery.Add($"page={currentPage - 1}");
            links.Add("prev", $"{baseUrl}?{string.Join("&", prevQuery)}");
        }

        if (currentPage < totalPages)
        {
            var nextQuery = queryParams.Where(p => !p.StartsWith("page=")).ToList();
            nextQuery.Add($"page={currentPage + 1}");
            links.Add("next", $"{baseUrl}?{string.Join("&", nextQuery)}");
        }

        var firstQuery = queryParams.Where(p => !p.StartsWith("page=")).ToList();
        firstQuery.Add("page=1");
        links.Add("first", $"{baseUrl}?{string.Join("&", firstQuery)}");
        
        var lastQuery = queryParams.Where(p => !p.StartsWith("page=")).ToList();
        lastQuery.Add($"page={totalPages}");
        links.Add("last", $"{baseUrl}?{string.Join("&", lastQuery)}");

        return links;
    }
}

