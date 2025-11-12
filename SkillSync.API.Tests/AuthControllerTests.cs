using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace SkillSync.API.Tests;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var registerRequest = new
        {
            Nome = $"Test User {Guid.NewGuid()}",
            Email = $"test{Guid.NewGuid()}@test.com",
            Senha = "Test123!",
            Role = "FREELANCER"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var jsonResponse = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        jsonResponse.TryGetProperty("email", out _).Should().BeTrue();
        jsonResponse.TryGetProperty("role", out _).Should().BeTrue();
        jsonResponse.TryGetProperty("message", out _).Should().BeTrue();
        jsonResponse.TryGetProperty("token", out _).Should().BeFalse(); // Registro não retorna token
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var registerRequest = new
        {
            Nome = "Test User",
            Email = "invalid-email",
            Senha = "Test123!",
            Role = "FREELANCER"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            Email = "nonexistent@test.com",
            Senha = "WrongPassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}

