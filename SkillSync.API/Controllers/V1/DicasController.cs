using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSync.API.Data;
using SkillSync.API.DTOs.Dica;

namespace SkillSync.API.Controllers.V1;

[ApiController]
[Route("api/v{version:apiVersion}/dicas")]
[Authorize]
[Produces("application/json")]
[ApiVersion("1.0")]
public class DicasController : ControllerBase
{
    private readonly SkillSyncDbContext _context;
    private readonly ILogger<DicasController> _logger;

    public DicasController(
        SkillSyncDbContext context,
        ILogger<DicasController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as dicas geradas pela IA
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<DicaResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DicaResponseDto>>> GetDicas()
    {
        try
        {
            var dicas = await _context.TGsDicasIas
                .OrderByDescending(d => d.DtGeracao)
                .ToListAsync();

            var dicasDto = dicas.Select(d => new DicaResponseDto
            {
                IdDica = d.IdDica,
                Titulo = d.DsTitulo,
                Conteudo = d.DsConteudo,
                DataGeracao = d.DtGeracao,
                IdAdminGerador = d.IdAdminGerador
            }).ToList();

            return Ok(dicasDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar dicas");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Busca dica por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DicaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DicaResponseDto>> GetDica(decimal id)
    {
        try
        {
            var dica = await _context.TGsDicasIas
                .FirstOrDefaultAsync(d => d.IdDica == id);

            if (dica == null)
            {
                return NotFound(new { message = "Dica não encontrada" });
            }

            var dicaDto = new DicaResponseDto
            {
                IdDica = dica.IdDica,
                Titulo = dica.DsTitulo,
                Conteudo = dica.DsConteudo,
                DataGeracao = dica.DtGeracao,
                IdAdminGerador = dica.IdAdminGerador
            };

            return Ok(dicaDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar dica {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }
}

