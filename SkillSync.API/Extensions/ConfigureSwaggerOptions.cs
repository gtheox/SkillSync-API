using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SkillSync.API.Extensions;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        // Configure Swagger documents for each API version discovered by ApiExplorer
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            var versionInfo = new OpenApiInfo
            {
                Title = "SkillSync API",
                Version = description.ApiVersion.ToString(),
                Description = description.ApiVersion.MajorVersion switch
                {
                    1 => "API RESTful para a plataforma SkillSync - Matchmaking de Freelancers (Versão 1.0 - Versão inicial)",
                    2 => "API RESTful para a plataforma SkillSync - Matchmaking de Freelancers (Versão 2.0 - Com filtros avançados e ML.NET melhorado)",
                    _ => $"API RESTful para a plataforma SkillSync - Versão {description.ApiVersion}"
                },
                Contact = new OpenApiContact
                {
                    Name = "SkillSync Team",
                    Email = "support@skillsync.com"
                }
            };

            if (description.IsDeprecated)
            {
                versionInfo.Description += " [DEPRECATED]";
            }

            // Create SwaggerDoc with GroupName from ApiExplorer (e.g., "v1.0", "v2.0")
            options.SwaggerDoc(description.GroupName, versionInfo);
        }
        
        // CRITICAL: Configure DocInclusionPredicate to filter endpoints by API version
        // The ApiExplorer sets GroupName for each endpoint based on [ApiVersion] attribute
        // The GroupNameFormat "'v'VVV" produces GroupNames like "v1.0", "v2.0"
        // Swagger filters endpoints by matching SwaggerDoc name with ApiExplorer GroupName
        options.DocInclusionPredicate((docName, apiDesc) =>
        {
            // The ApiExplorer automatically sets GroupName based on the [ApiVersion] attribute
            // Match the SwaggerDoc name (e.g., "v1.0") with the GroupName from ApiExplorer
            if (!apiDesc.TryGetMethodInfo(out var methodInfo))
            {
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(apiDesc.GroupName))
            {
                return false;
            }
            
            // Match SwaggerDoc name with GroupName from ApiExplorer (case-insensitive)
            // This ensures only endpoints from the correct API version appear in each SwaggerDoc
            var matches = apiDesc.GroupName.Equals(docName, StringComparison.OrdinalIgnoreCase);
            return matches;
        });
    }
}

