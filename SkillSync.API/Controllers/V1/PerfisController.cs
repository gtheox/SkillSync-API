using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using SkillSync.API.Data;
using SkillSync.API.DTOs.Perfil;
using SkillSync.API.Models;
using System.Security.Claims;

namespace SkillSync.API.Controllers.V1;

[ApiController]
[Route("api/v{version:apiVersion}/perfis")]
[Authorize]
[Produces("application/json")]
[ApiVersion("1.0")]
public class PerfisController : ControllerBase
{
    private readonly SkillSyncDbContext _context;
    private readonly ILogger<PerfisController> _logger;

    public PerfisController(
        SkillSyncDbContext context,
        ILogger<PerfisController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os perfis
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PerfilResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PerfilResponseDto>>> GetPerfis()
    {
        try
        {
            var perfis = await _context.TGsPerfisFreelancers
                .Include(p => p.IdUsuarioNavigation)
                .Include(p => p.TGsPerfilHabilidades)
                    .ThenInclude(ph => ph.IdHabilidadeNavigation)
                .ToListAsync();

            var perfisDto = perfis.Select(p => MapToDto(p, Request)).ToList();

            return Ok(perfisDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar perfis");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Busca perfil por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PerfilResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PerfilResponseDto>> GetPerfil(decimal id)
    {
        try
        {
            var perfil = await _context.TGsPerfisFreelancers
                .Include(p => p.IdUsuarioNavigation)
                .Include(p => p.TGsPerfilHabilidades)
                    .ThenInclude(ph => ph.IdHabilidadeNavigation)
                .FirstOrDefaultAsync(p => p.IdPerfil == id);

            if (perfil == null)
            {
                return NotFound(new { message = "Perfil não encontrado" });
            }

            return Ok(MapToDto(perfil, Request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar perfil {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Cria um novo perfil
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PerfilResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PerfilResponseDto>> CreatePerfil([FromBody] PerfilCreateDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();

            // Verificar se o usuário já tem perfil
            var perfilExistente = await _context.TGsPerfisFreelancers
                .FirstOrDefaultAsync(p => p.IdUsuario == userId);

            if (perfilExistente != null)
            {
                return BadRequest(new { message = "Usuário já possui um perfil" });
            }

            // Chamar procedure do Oracle para criar perfil
            var perfil = new TGsPerfisFreelancer
            {
                IdUsuario = userId,
                DsTituloProfissional = dto.TituloProfissional,
                DsResumo = dto.Resumo,
                VlHora = dto.ValorHora,
                DtUltimaAtualizacao = DateTime.Now
            };

            _context.TGsPerfisFreelancers.Add(perfil);
            await _context.SaveChangesAsync();

            // Recarregar a entidade para obter o ID gerado pelo Oracle
            await _context.Entry(perfil).ReloadAsync();
            var idPerfilGerado = perfil.IdPerfil;

            // Adicionar habilidades usando stored procedure
            if (dto.Habilidades != null && dto.Habilidades.Any())
            {
                // Validar se todas as habilidades existem
                var habilidadesExistentes = await _context.TGsHabilidades
                    .Where(h => dto.Habilidades.Contains(h.IdHabilidade))
                    .Select(h => h.IdHabilidade)
                    .ToListAsync();
                
                var habilidadesInvalidas = dto.Habilidades
                    .Except(habilidadesExistentes)
                    .ToList();
                
                if (habilidadesInvalidas.Any())
                {
                    _logger.LogWarning("Tentativa de criar perfil com habilidades inválidas: {Habilidades}", 
                        string.Join(", ", habilidadesInvalidas));
                    return BadRequest(new { 
                        message = $"Habilidades inválidas: {string.Join(", ", habilidadesInvalidas)}" 
                    });
                }
                
                // Usar stored procedure para adicionar habilidades
                foreach (var habilidadeId in dto.Habilidades)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "BEGIN PKG_GERENCIAMENTO.SP_ADICIONAR_HABILIDADE_PERFIL(:p_idPerfil, :p_idHabilidade); END;",
                        new Oracle.ManagedDataAccess.Client.OracleParameter("p_idPerfil", idPerfilGerado),
                        new Oracle.ManagedDataAccess.Client.OracleParameter("p_idHabilidade", habilidadeId));
                }
            }

            // Buscar perfil completo
            var perfilCompleto = await _context.TGsPerfisFreelancers
                .Include(p => p.IdUsuarioNavigation)
                .Include(p => p.TGsPerfilHabilidades)
                    .ThenInclude(ph => ph.IdHabilidadeNavigation)
                .FirstOrDefaultAsync(p => p.IdPerfil == idPerfilGerado);

            return CreatedAtAction(nameof(GetPerfil), new { id = idPerfilGerado }, 
                MapToDto(perfilCompleto!, Request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar perfil");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Atualiza um perfil existente
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PerfilResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PerfilResponseDto>> UpdatePerfil(decimal id, [FromBody] PerfilCreateDto dto)
    {
        try
        {
            var perfil = await _context.TGsPerfisFreelancers
                .FirstOrDefaultAsync(p => p.IdPerfil == id);

            if (perfil == null)
            {
                return NotFound(new { message = "Perfil não encontrado" });
            }

            // Verificar permissões: admin pode editar qualquer perfil, freelancer apenas o seu
            var userId = GetUserId();
            var isAdmin = IsAdmin();
            if (!isAdmin && perfil.IdUsuario != userId)
            {
                return Forbid();
            }

            if (dto.TituloProfissional != null) perfil.DsTituloProfissional = dto.TituloProfissional;
            if (dto.Resumo != null) perfil.DsResumo = dto.Resumo;
            if (dto.ValorHora != null) perfil.VlHora = dto.ValorHora;
            perfil.DtUltimaAtualizacao = DateTime.Now;

            await _context.SaveChangesAsync();

            var perfilCompleto = await _context.TGsPerfisFreelancers
                .Include(p => p.IdUsuarioNavigation)
                .Include(p => p.TGsPerfilHabilidades)
                    .ThenInclude(ph => ph.IdHabilidadeNavigation)
                .FirstOrDefaultAsync(p => p.IdPerfil == id);

            return Ok(MapToDto(perfilCompleto!, Request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar perfil {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Deleta um perfil
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeletePerfil(decimal id)
    {
        try
        {
            var perfil = await _context.TGsPerfisFreelancers
                .FirstOrDefaultAsync(p => p.IdPerfil == id);

            if (perfil == null)
            {
                return NotFound(new { message = "Perfil não encontrado" });
            }

            // Verificar se o usuário é o dono do perfil
            var userId = GetUserId();
            if (perfil.IdUsuario != userId)
            {
                return Forbid();
            }

            // Deletar registros filhos primeiro (habilidades do perfil)
            var habilidadesPerfil = await _context.TGsPerfilHabilidades
                .Where(ph => ph.IdPerfil == id)
                .ToListAsync();
            
            if (habilidadesPerfil.Any())
            {
                _context.TGsPerfilHabilidades.RemoveRange(habilidadesPerfil);
                await _context.SaveChangesAsync();
            }

            // Agora pode deletar o perfil
            _context.TGsPerfisFreelancers.Remove(perfil);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar perfil {Id}", id);
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

    private static PerfilResponseDto MapToDto(TGsPerfisFreelancer perfil, HttpRequest request)
    {
        var baseUrl = $"{request.Scheme}://{request.Host}";
        var dto = new PerfilResponseDto
        {
            IdPerfil = perfil.IdPerfil,
            IdUsuario = perfil.IdUsuario,
            TituloProfissional = perfil.DsTituloProfissional,
            Resumo = perfil.DsResumo,
            ValorHora = perfil.VlHora,
            DataUltimaAtualizacao = perfil.DtUltimaAtualizacao,
            Habilidades = perfil.TGsPerfilHabilidades
                .Select(ph => ph.IdHabilidadeNavigation.NmHabilidade)
                .ToList(),
            Links = new Dictionary<string, string>
            {
                { "self", $"{baseUrl}/api/v1/perfis/{perfil.IdPerfil}" },
                { "edit", $"{baseUrl}/api/v1/perfis/{perfil.IdPerfil}" },
                { "delete", $"{baseUrl}/api/v1/perfis/{perfil.IdPerfil}" }
            }
        };
        return dto;
    }
}

