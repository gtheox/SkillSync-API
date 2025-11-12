using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using SkillSync.API.Data;
using SkillSync.API.DTOs.Auth;
using SkillSync.API.Helpers;

namespace SkillSync.API.Services;

public class AuthService : IAuthService
{
    private readonly SkillSyncDbContext _context;
    private readonly JwtHelper _jwtHelper;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        SkillSyncDbContext context,
        JwtHelper jwtHelper,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtHelper = jwtHelper;
        _logger = logger;
    }

    public async Task<RegisterResponse?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Normalizar email (lowercase e trim)
            var emailNormalized = request.Email.Trim().ToLowerInvariant();

            // Verificar se o email já existe
            // Como estamos salvando em lowercase, buscamos diretamente
            var existingUser = await _context.TGsUsuarios
                .FirstOrDefaultAsync(u => u.DsEmail == emailNormalized);

            if (existingUser != null)
            {
                _logger.LogWarning("Tentativa de registro com email já existente: {Email}", emailNormalized);
                return null;
            }

            // Chamar a procedure do Oracle usando ExecuteSqlRaw
            // A procedure não faz hash, então precisamos fazer aqui antes de passar
            var senhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha);
            var roleUpper = request.Role.ToUpper().Trim();

            await _context.Database.ExecuteSqlRawAsync(
                "BEGIN PKG_USUARIO.SP_REGISTRAR_USUARIO(:p_nome, :p_email, :p_senha, :p_role); END;",
                new OracleParameter("p_nome", request.Nome.Trim()),
                new OracleParameter("p_email", emailNormalized),
                new OracleParameter("p_senha", senhaHash),
                new OracleParameter("p_role", roleUpper));

            // Buscar o usuário recém-criado
            // Como salvamos em lowercase, buscamos diretamente
            var newUser = await _context.TGsUsuarios
                .FirstOrDefaultAsync(u => u.DsEmail == emailNormalized);

            if (newUser == null)
            {
                _logger.LogError("Falha ao criar usuário após chamada da procedure");
                return null;
            }

            _logger.LogInformation("Usuário registrado com sucesso: {Email}", emailNormalized);

            // Não retornar token no cadastro - apenas informações do usuário
            return new RegisterResponse
            {
                Email = newUser.DsEmail,
                Role = newUser.FlRole,
                IdUsuario = newUser.IdUsuario,
                Message = "Usuário registrado com sucesso. Faça login para obter o token."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar usuário: {Email}", request.Email);
            throw;
        }
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            // Normalizar email (lowercase e trim)
            var emailNormalized = request.Email.Trim().ToLowerInvariant();

            // Buscar usuário com email (case-insensitive usando SQL)
            // Usa FormattableString para evitar SQL injection
            var emailParam = emailNormalized;
            var user = await _context.TGsUsuarios
                .FromSqlInterpolated($@"
                    SELECT * FROM T_GS_USUARIOS 
                    WHERE LOWER(TRIM(DS_EMAIL)) = LOWER(TRIM({emailParam}))
                ")
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("Tentativa de login com email não encontrado: {Email}", emailNormalized);
                return null;
            }

            // Verificar senha com BCrypt
            try
            {
                var senhaValida = BCrypt.Net.BCrypt.Verify(request.Senha, user.DsSenha);
                
                if (!senhaValida)
                {
                    _logger.LogWarning("Tentativa de login com senha incorreta para: {Email}", emailNormalized);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar senha para: {Email}. Erro: {ErrorMessage}", emailNormalized, ex.Message);
                return null;
            }

            // Gerar token JWT
            var token = _jwtHelper.GenerateToken(user.DsEmail, user.FlRole, user.IdUsuario);

            _logger.LogInformation("Login realizado com sucesso: {Email} (ID: {IdUsuario})", emailNormalized, user.IdUsuario);

            return new AuthResponse
            {
                Token = token,
                Email = user.DsEmail,
                Role = user.FlRole,
                IdUsuario = user.IdUsuario
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer login: {Email}. Erro: {ErrorMessage}", request.Email, ex.Message);
            throw;
        }
    }
}

