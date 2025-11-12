using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SkillSync.API.DTOs.AI;

namespace SkillSync.API.Services;

public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIService> _logger;
    private readonly string _apiUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public AIService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AIService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiUrl = configuration["AI:ApiUrl"] ?? throw new ArgumentNullException("AI:ApiUrl");
        
        // Configurar opções JSON para respeitar JsonPropertyName (snake_case)
        // Não usar política de nomenclatura para que os atributos JsonPropertyName tenham prioridade
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // Não usar política de nomenclatura
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<MatchResponse> GerarMatchesAsync(MatchRequest request)
    {
        try
        {
            _logger.LogInformation("Enviando requisição para API de IA: {Url} com {PerfilCount} perfis", 
                _apiUrl, request.Perfis.Count);

            // Serializar request manualmente com opções JSON que respeitam JsonPropertyName
            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            _logger.LogInformation("Payload JSON enviado para API de IA (snake_case): {Payload}", requestJson);

            // Criar request HTTP manualmente para garantir que usa as opções JSON corretas
            var jsonContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiUrl, jsonContent);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro na API de IA: {StatusCode} - {Content}", response.StatusCode, responseContent);
                
                // Tentar deserializar o erro para mostrar mensagem mais clara
                try
                {
                    var errorJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);
                    if (errorJson.TryGetProperty("detail", out var detail))
                    {
                        _logger.LogError("Detalhes do erro da API de IA: {Detail}", detail.ToString());
                    }
                }
                catch
                {
                    // Se não conseguir deserializar, apenas logar o conteúdo bruto
                }
                
                throw new HttpRequestException($"Erro na API de IA: {response.StatusCode} - {responseContent}");
            }

            _logger.LogDebug("Resposta da API de IA: {Response}", responseContent);

            // Deserializar resposta manualmente com as mesmas opções JSON
            var matchResponse = JsonSerializer.Deserialize<MatchResponse>(responseContent, _jsonOptions);

            if (matchResponse == null)
            {
                _logger.LogError("Resposta da API de IA é nula. Conteúdo: {Content}", responseContent);
                throw new InvalidOperationException("Resposta da API de IA é nula");
            }

            _logger.LogInformation("Matches gerados com sucesso: {Count} perfis", matchResponse.Matches.Count);

            return matchResponse;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout ao chamar API de IA após {Timeout} segundos", _httpClient.Timeout.TotalSeconds);
            throw new HttpRequestException($"Timeout ao chamar API de IA (timeout: {_httpClient.Timeout.TotalSeconds}s)", ex);
        }
        catch (HttpRequestException)
        {
            // Re-throw HttpRequestException para manter a mensagem original
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao chamar API de IA: {ErrorMessage}", ex.Message);
            throw;
        }
    }
}

