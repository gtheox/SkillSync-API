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
        const int maxRetries = 3;
        const int baseDelaySeconds = 5; // Delay inicial de 5 segundos (para cold start)
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Enviando requisição para API de IA (tentativa {Attempt}/{MaxRetries}): {Url} com {PerfilCount} perfis", 
                    attempt, maxRetries, _apiUrl, request.Perfis.Count);

                // Serializar request manualmente com opções JSON que respeitam JsonPropertyName
                var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
                _logger.LogDebug("Payload JSON enviado para API de IA (snake_case): {Payload}", requestJson);

                // Criar request HTTP manualmente para garantir que usa as opções JSON corretas
                var jsonContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_apiUrl, jsonContent);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
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

                // Tratar erros que podem ser temporários (cold start, rate limiting)
                var isRetryableError = response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                                     response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                                     response.StatusCode == System.Net.HttpStatusCode.GatewayTimeout ||
                                     response.StatusCode == System.Net.HttpStatusCode.BadGateway ||
                                     (response.StatusCode == System.Net.HttpStatusCode.InternalServerError && attempt < maxRetries);

                _logger.LogWarning("Erro na API de IA: {StatusCode} - {Content} (tentativa {Attempt}/{MaxRetries})", 
                    response.StatusCode, responseContent, attempt, maxRetries);

                // Tentar deserializar o erro para mostrar mensagem mais clara
                try
                {
                    var errorJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);
                    if (errorJson.TryGetProperty("detail", out var detail))
                    {
                        _logger.LogWarning("Detalhes do erro da API de IA: {Detail}", detail.ToString());
                    }
                }
                catch
                {
                    // Se não conseguir deserializar, apenas logar o conteúdo bruto
                }

                // Se não for um erro que pode ser retentado, ou se já tentamos todas as vezes, lançar exceção
                if (!isRetryableError || attempt == maxRetries)
                {
                    throw new HttpRequestException($"Erro na API de IA: {response.StatusCode} - {responseContent}");
                }

                // Aguardar antes de tentar novamente (backoff exponencial)
                var delaySeconds = baseDelaySeconds * attempt;
                _logger.LogInformation("Aguardando {DelaySeconds}s antes de tentar novamente (cold start ou rate limit)...", delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
            catch (TaskCanceledException ex)
            {
                if (attempt == maxRetries)
                {
                    _logger.LogError(ex, "Timeout ao chamar API de IA após {Timeout} segundos (todas as tentativas esgotadas)", _httpClient.Timeout.TotalSeconds);
                    throw new HttpRequestException($"Timeout ao chamar API de IA (timeout: {_httpClient.Timeout.TotalSeconds}s). A API de IA pode estar em cold start.", ex);
                }
                
                // Timeout também pode ser causado por cold start, tentar novamente
                var delaySeconds = baseDelaySeconds * attempt;
                _logger.LogWarning("Timeout na tentativa {Attempt}/{MaxRetries}. Aguardando {DelaySeconds}s antes de tentar novamente (pode ser cold start)...", 
                    attempt, maxRetries, delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
            catch (HttpRequestException ex)
            {
                // Se já tentamos todas as vezes, re-lançar a exceção
                if (attempt == maxRetries)
                {
                    throw;
                }
                
                // Verificar se a mensagem indica um erro temporário
                if (ex.Message.Contains("Too Many Requests") || 
                    ex.Message.Contains("Service Unavailable") ||
                    ex.Message.Contains("Timeout"))
                {
                    var delaySeconds = baseDelaySeconds * attempt;
                    _logger.LogWarning("Erro temporário na tentativa {Attempt}/{MaxRetries}: {Error}. Aguardando {DelaySeconds}s antes de tentar novamente...", 
                        attempt, maxRetries, ex.Message, delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
                else
                {
                    // Se não for um erro temporário, lançar exceção imediatamente
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao chamar API de IA: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        // Não deveria chegar aqui, mas se chegou, lançar exceção genérica
        throw new HttpRequestException("Falha ao chamar API de IA após todas as tentativas");
    }
}

