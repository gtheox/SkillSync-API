using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSync.API.Data;
using SkillSync.API.DTOs.AI;
using SkillSync.API.Services;

namespace SkillSync.API.Controllers.V1;

[ApiController]
[Route("api/v{version:apiVersion}/projetos/{idProjeto}/gerar-matches")]
[Authorize]
[Produces("application/json")]
[ApiVersion("1.0")]
public class MatchesController : ControllerBase
{
    private readonly SkillSyncDbContext _context;
    private readonly IAIService _aiService;
    private readonly ILogger<MatchesController> _logger;

    public MatchesController(
        SkillSyncDbContext context,
        IAIService aiService,
        ILogger<MatchesController> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// Gera matches entre um projeto e perfis de freelancers usando IA
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchResponse>> GerarMatches(decimal idProjeto)
    {
        try
        {
            // Buscar projeto
            var projeto = await _context.TGsProjetosContratantes
                .Include(p => p.TGsProjetoRequisitos)
                    .ThenInclude(pr => pr.IdHabilidadeNavigation)
                .FirstOrDefaultAsync(p => p.IdProjeto == idProjeto);

            if (projeto == null)
            {
                return NotFound(new { message = "Projeto não encontrado" });
            }

            // Buscar todos os perfis de freelancers
            var perfis = await _context.TGsPerfisFreelancers
                .Include(p => p.TGsPerfilHabilidades)
                    .ThenInclude(ph => ph.IdHabilidadeNavigation)
                .ToListAsync();

            if (!perfis.Any())
            {
                return Ok(new MatchResponse { Matches = new List<MatchItem>() });
            }

            // Montar request para API de IA
            var matchRequest = new MatchRequest
            {
                Projeto = new ProjetoDto
                {
                    Titulo = projeto.DsTitulo,
                    Descricao = projeto.DsDescricao
                },
                Perfis = perfis.Select(p => new PerfilDto
                {
                    IdPerfil = (int)p.IdPerfil, // Converter decimal para int (API Python espera int)
                    TituloProfissional = p.DsTituloProfissional ?? "",
                    Resumo = p.DsResumo ?? "",
                    Habilidades = p.TGsPerfilHabilidades
                        .Select(ph => ph.IdHabilidadeNavigation.NmHabilidade)
                        .ToList()
                }).ToList()
            };

            // Chamar API de IA
            var matchResponse = await _aiService.GerarMatchesAsync(matchRequest);

            _logger.LogInformation("Matches gerados para projeto {IdProjeto}: {Count} perfis", 
                idProjeto, matchResponse.Matches.Count);

            return Ok(matchResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar matches para projeto {IdProjeto}", idProjeto);
            return StatusCode(500, new { message = "Erro ao gerar matches: " + ex.Message });
        }
    }
}

