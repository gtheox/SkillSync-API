using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSync.API.Data;
using SkillSync.API.DTOs.Habilidade;

namespace SkillSync.API.Controllers.V1;

[ApiController]
[Route("api/v{version:apiVersion}/habilidades")]
[Produces("application/json")]
[ApiVersion("1.0")]
public class HabilidadesController : ControllerBase
{
    private readonly SkillSyncDbContext _context;
    private readonly ILogger<HabilidadesController> _logger;

    public HabilidadesController(
        SkillSyncDbContext context,
        ILogger<HabilidadesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as habilidades disponíveis
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<HabilidadeResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<HabilidadeResponseDto>>> GetHabilidades()
    {
        try
        {
            var habilidades = await _context.TGsHabilidades
                .OrderBy(h => h.NmHabilidade)
                .ToListAsync();

            var habilidadesDto = habilidades.Select(h => new HabilidadeResponseDto
            {
                IdHabilidade = h.IdHabilidade,
                Nome = h.NmHabilidade
            }).ToList();

            return Ok(habilidadesDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar habilidades");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Busca habilidade por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(HabilidadeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HabilidadeResponseDto>> GetHabilidade(decimal id)
    {
        try
        {
            var habilidade = await _context.TGsHabilidades
                .FirstOrDefaultAsync(h => h.IdHabilidade == id);

            if (habilidade == null)
            {
                return NotFound(new { message = "Habilidade não encontrada" });
            }

            return Ok(new HabilidadeResponseDto
            {
                IdHabilidade = habilidade.IdHabilidade,
                Nome = habilidade.NmHabilidade
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar habilidade {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }
}

