using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SkillSync.API.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SkillSync.API.Tests;

public class ProjetosControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly SkillSyncDbContext _context;
    private string? _token;

    public ProjetosControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();

        var scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<SkillSyncDbContext>();

        // Autenticar antes de executar os testes
        AuthenticateAsync().Wait();
    }

    private async Task AuthenticateAsync()
    {
        // Registrar um usuário de teste (se não existir)
        var registerRequest = new
        {
            Nome = "Test User",
            Email = "test@test.com",
            Senha = "Test123!",
            Role = "CONTRATANTE"
        };

        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Sempre fazer login para obter token (registro não retorna token)
        var loginRequest = new
        {
            Email = "test@test.com",
            Senha = "Test123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        if (loginResponse.IsSuccessStatusCode)
        {
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
            _token = authResponse.GetProperty("token").GetString();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
        }
    }

    [Fact]
    public async Task GetProjetos_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/projetos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetProjetos_WithPagination_ShouldReturnPagedResponse()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/projetos?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        pagedResponse.GetProperty("page").GetInt32().Should().Be(1);
        pagedResponse.GetProperty("pageSize").GetInt32().Should().Be(10);
        pagedResponse.GetProperty("data").GetArrayLength().Should().BeLessOrEqualTo(10);
    }

    [Fact]
    public async Task CreateProjeto_ShouldReturnCreated()
    {
        // Arrange
        var projeto = new
        {
            Titulo = "Projeto de Teste",
            Descricao = "Descrição do projeto de teste",
            Orcamento = 1000.0m,
            HabilidadesRequisitadas = new List<decimal>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/projetos", projeto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdProjeto = await response.Content.ReadFromJsonAsync<JsonElement>();
        createdProjeto.GetProperty("titulo").GetString().Should().Be(projeto.Titulo);
    }

    [Fact]
    public async Task GetProjeto_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/projetos/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProjeto_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/v1/projetos/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}

