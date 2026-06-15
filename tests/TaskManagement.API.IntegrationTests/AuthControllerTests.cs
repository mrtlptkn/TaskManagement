using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.Features.Auth.Commands.Login;
using TaskManagement.Application.Features.Auth.Commands.Register;
using TaskManagement.Domain.Entities;
using Xunit;

namespace TaskManagement.API.IntegrationTests;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldReturn200_AndUserId_WhenDataIsValid()
    {
        var command = new RegisterCommand
        {
            FullName = "Test Kullanıcı",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "Test123!",
            Role = Roles.Employee
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<RegisterResultDto>();
        result!.UserId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_ShouldReturn400_WhenEmailIsInvalid()
    {
        var command = new RegisterCommand
        {
            FullName = "Test Kullanıcı",
            Email = "gecersiz-email",
            Password = "Test123!",
            Role = Roles.Employee
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ShouldReturnToken_WhenCredentialsAreValid()
    {
        var email = $"login_{Guid.NewGuid():N}@example.com";
        var password = "Test123!";

        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand
        {
            FullName = "Giriş Testi",
            Email = email,
            Password = password,
            Role = Roles.Manager
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand
        {
            Email = email,
            Password = password
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResultDto>();
        result!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ShouldReturn401_WhenPasswordIsWrong()
    {
        var email = $"wrongpass_{Guid.NewGuid():N}@example.com";

        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand
        {
            FullName = "Yanlış Şifre Testi",
            Email = email,
            Password = "Correct123!",
            Role = Roles.Employee
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand
        {
            Email = email,
            Password = "WrongPassword!"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
