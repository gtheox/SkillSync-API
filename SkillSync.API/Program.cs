using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SkillSync.API.Data;
using SkillSync.API.Extensions;
using SkillSync.API.Helpers;
using SkillSync.API.Services;
using System.Text;
using System.Linq;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configurar JSON para usar camelCase por padrão
        // Mas os atributos JsonPropertyName terão prioridade
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;
    });

// Configure API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader("version"),
        new HeaderApiVersionReader("X-Version"),
        new UrlSegmentApiVersionReader()
    );
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; // Format: v1.0, v2.0, etc.
    options.SubstituteApiVersionInUrl = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
});

// Configure Oracle DbContext
var connectionString = builder.Configuration.GetConnectionString("OracleConnection");
builder.Services.AddDbContext<SkillSyncDbContext>(options =>
    options.UseOracle(connectionString));

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RequireExpirationTime = true,
        RequireSignedTokens = true
    };
    
    // Adicionar eventos para logging de erros de autenticação
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();
            logger.LogError(context.Exception, 
                "Falha na autenticação JWT. Path: {Path}, Header: {Header}, Error: {Error}", 
                context.HttpContext.Request.Path, 
                string.IsNullOrEmpty(authHeader) ? "Nenhum header Authorization" : "Header presente",
                context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Token JWT validado com sucesso para: {Email}", 
                context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();
            logger.LogWarning(
                "Desafio de autenticação JWT. Path: {Path}, Header: {Header}, Error: {Error}, ErrorDescription: {ErrorDescription}", 
                context.HttpContext.Request.Path,
                string.IsNullOrEmpty(authHeader) ? "Nenhum header Authorization" : "Header presente",
                context.Error,
                context.ErrorDescription);
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();
            logger.LogDebug("Mensagem JWT recebida. Path: {Path}, Header presente: {HasHeader}", 
                context.HttpContext.Request.Path,
                !string.IsNullOrEmpty(authHeader));
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Register services
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMLService, MLService>();

// Configure HttpClient for AI Service
// AddHttpClient já registra o serviço como scoped, não precisa AddScoped separado
builder.Services.AddHttpClient<IAIService, AIService>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var timeout = int.Parse(configuration["AI:TimeoutInSeconds"] ?? "30");
    client.Timeout = TimeSpan.FromSeconds(timeout);
});

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SkillSyncDbContext>("oracle", tags: new[] { "database" });

// Configure Swagger/OpenAPI with versioning
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Configure JWT in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme. 
                      Enter your token in the text input below (without 'Bearer' prefix).
                      Example: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    
    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
    
    // NOTE: DocInclusionPredicate will be configured in ConfigureSwaggerOptions
    // This ensures documents are created first, then filtering is applied
});

// Configure Swagger documents dynamically using IApiVersionDescriptionProvider
// This must be done using IConfigureOptions to access IApiVersionDescriptionProvider after services are built
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Use IApiVersionDescriptionProvider to configure Swagger UI endpoints
        // This will automatically discover all API versions from controllers
        var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        
        // Ordenar versões para que v1 apareça primeiro
        var descriptions = apiVersionDescriptionProvider.ApiVersionDescriptions
            .OrderBy(d => d.ApiVersion)
            .ToList();
        
        // Adicionar v1.0 primeiro (será a padrão)
        var v1Description = descriptions.FirstOrDefault(d => d.ApiVersion.MajorVersion == 1);
        if (v1Description != null)
        {
            c.SwaggerEndpoint(
                $"/swagger/{v1Description.GroupName}/swagger.json",
                $"SkillSync API {v1Description.ApiVersion} (Padrão)");
        }
        
        // Adicionar outras versões
        foreach (var description in descriptions.Where(d => d.ApiVersion.MajorVersion != 1))
        {
            c.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                $"SkillSync API {description.ApiVersion}");
        }
        
        // Configurações do Swagger UI
        c.DefaultModelsExpandDepth(-1);
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
        c.EnableDeepLinking(); // Permite links diretos para endpoints
        c.EnableFilter(); // Habilita filtro de endpoints
    });
}

app.UseSerilogRequestLogging();

// HTTPS Redirection pode causar problemas se a API está rodando apenas em HTTP
// Comentado temporariamente para testes
// app.UseHttpsRedirection();

app.UseCors("AllowAll");

// IMPORTANTE: UseAuthentication deve vir antes de UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Configure Health Check endpoint with detailed information
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        
        var healthCheckResult = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            version = "1.0.0",
            environment = app.Environment.EnvironmentName,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                exception = e.Value.Exception?.Message,
                duration = Math.Round(e.Value.Duration.TotalMilliseconds, 2),
                data = e.Value.Data
            }).ToList(),
            summary = new
            {
                total = report.Entries.Count,
                healthy = report.Entries.Count(e => e.Value.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy),
                degraded = report.Entries.Count(e => e.Value.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded),
                unhealthy = report.Entries.Count(e => e.Value.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy)
            }
        };
        
        var result = System.Text.Json.JsonSerializer.Serialize(healthCheckResult, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        await context.Response.WriteAsync(result);
    }
});

app.Run();
