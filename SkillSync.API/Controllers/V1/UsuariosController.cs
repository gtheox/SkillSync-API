using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSync.API.Data;
using SkillSync.API.DTOs.Usuario;
using SkillSync.API.Models;
using System.Security.Claims;
using BCrypt.Net;

namespace SkillSync.API.Controllers.V1;

[ApiController]
[Route("api/v{version:apiVersion}/usuarios")]
[Authorize]
[Produces("application/json")]
[ApiVersion("1.0")]
public class UsuariosController : ControllerBase
{
    private readonly SkillSyncDbContext _context;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(
        SkillSyncDbContext context,
        ILogger<UsuariosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os usuários (apenas ADMIN)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<UsuarioResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UsuarioResponseDto>>> GetUsuarios()
    {
        try
        {
            // Verificar se é admin
            if (!IsAdmin())
            {
                return Forbid();
            }

            var usuarios = await _context.TGsUsuarios
                .OrderBy(u => u.NmUsuario)
                .ToListAsync();

            var usuariosDto = usuarios.Select(u => MapToDto(u, Request)).ToList();

            return Ok(usuariosDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar usuários");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Busca usuário por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UsuarioResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioResponseDto>> GetUsuario(decimal id)
    {
        try
        {
            var usuario = await _context.TGsUsuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound(new { message = "Usuário não encontrado" });
            }

            // Verificar permissões: admin pode ver qualquer usuário, outros apenas o próprio
            var userId = GetUserId();
            if (!IsAdmin() && usuario.IdUsuario != userId)
            {
                return Forbid();
            }

            return Ok(MapToDto(usuario, Request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar usuário {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Cria um novo usuário (apenas ADMIN)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UsuarioResponseDto>> CreateUsuario([FromBody] UsuarioCreateDto dto)
    {
        try
        {
            // Verificar se é admin
            if (!IsAdmin())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verificar se email já existe
            var emailNormalized = dto.Email.Trim().ToLowerInvariant();
            var usuarioExistente = await _context.TGsUsuarios
                .FirstOrDefaultAsync(u => u.DsEmail.ToLower() == emailNormalized);

            if (usuarioExistente != null)
            {
                return BadRequest(new { message = "Email já está em uso" });
            }

            // Criar novo usuário
            var usuario = new TGsUsuario
            {
                NmUsuario = dto.Nome.Trim(),
                DsEmail = emailNormalized,
                DsSenha = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
                FlRole = dto.Role.ToUpper(),
                DtCriacao = DateTime.Now
            };

            _context.TGsUsuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Recarregar para obter ID gerado
            await _context.Entry(usuario).ReloadAsync();

            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.IdUsuario }, MapToDto(usuario, Request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar usuário");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Atualiza um usuário
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UsuarioResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioResponseDto>> UpdateUsuario(decimal id, [FromBody] UsuarioUpdateDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var usuario = await _context.TGsUsuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound(new { message = "Usuário não encontrado" });
            }

            // Verificar permissões: admin pode editar qualquer usuário, outros apenas o próprio
            var userId = GetUserId();
            if (!IsAdmin() && usuario.IdUsuario != userId)
            {
                return Forbid();
            }

            // Atualizar campos
            if (dto.Nome != null) usuario.NmUsuario = dto.Nome.Trim();
            if (dto.Email != null)
            {
                var emailNormalized = dto.Email.Trim().ToLowerInvariant();
                // Verificar se email já está em uso por outro usuário
                var emailEmUso = await _context.TGsUsuarios
                    .FirstOrDefaultAsync(u => u.DsEmail.ToLower() == emailNormalized && u.IdUsuario != id);
                if (emailEmUso != null)
                {
                    return BadRequest(new { message = "Email já está em uso" });
                }
                usuario.DsEmail = emailNormalized;
            }
            if (dto.Senha != null) usuario.DsSenha = BCrypt.Net.BCrypt.HashPassword(dto.Senha);
            // Apenas admin pode alterar role
            if (dto.Role != null && IsAdmin())
            {
                usuario.FlRole = dto.Role.ToUpper();
            }

            await _context.SaveChangesAsync();

            return Ok(MapToDto(usuario, Request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar usuário {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Deleta um usuário (apenas ADMIN)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteUsuario(decimal id)
    {
        try
        {
            // Verificar se é admin
            if (!IsAdmin())
            {
                return Forbid();
            }

            var usuario = await _context.TGsUsuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound(new { message = "Usuário não encontrado" });
            }

            // Verificar se usuário tem perfis ou projetos associados
            var temPerfil = await _context.TGsPerfisFreelancers
                .AnyAsync(p => p.IdUsuario == id);
            var temProjetos = await _context.TGsProjetosContratantes
                .AnyAsync(p => p.IdUsuarioContratante == id);

            if (temPerfil || temProjetos)
            {
                return BadRequest(new { message = "Não é possível excluir usuário com perfis ou projetos associados" });
            }

            _context.TGsUsuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar usuário {Id}", id);
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

    private static UsuarioResponseDto MapToDto(TGsUsuario usuario, HttpRequest request)
    {
        var baseUrl = $"{request.Scheme}://{request.Host}";
        var dto = new UsuarioResponseDto
        {
            IdUsuario = usuario.IdUsuario,
            Nome = usuario.NmUsuario,
            Email = usuario.DsEmail,
            Role = usuario.FlRole,
            DataCriacao = usuario.DtCriacao,
            Links = new Dictionary<string, string>
            {
                { "self", $"{baseUrl}/api/v1/usuarios/{usuario.IdUsuario}" },
                { "edit", $"{baseUrl}/api/v1/usuarios/{usuario.IdUsuario}" },
                { "delete", $"{baseUrl}/api/v1/usuarios/{usuario.IdUsuario}" }
            }
        };
        return dto;
    }
}

