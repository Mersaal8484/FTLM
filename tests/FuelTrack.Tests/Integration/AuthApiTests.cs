using System.Net;
using System.Net.Http.Json;
using FuelTrack.Application.DTOs.Auth;
using FuelTrack.API.Models;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FuelTrack.Tests.Integration;

public class AuthApiTests : IntegrationTestBase
{
    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Test user is seeded in IntegrationTestBase.InitializeAsync
        var client = Factory.CreateClient();

        var request = new LoginRequest
        {
            UserName = "testadmin",
            Password = TestPassword
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));
        Assert.True(body.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();

        var request = new LoginRequest
        {
            UserName = "testadmin",
            Password = "WrongPassword123!"
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_NonExistentUser_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();

        var request = new LoginRequest
        {
            UserName = "nonexistentuser",
            Password = "DoesntMatter1!"
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_EmptyBody_ReturnsBadRequest()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsOkWithUser()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthMeResponse>();
        Assert.NotNull(body);
        Assert.Equal("testadmin", body.UserName);
        Assert.Equal("testadmin@test.com", body.Email);
        Assert.True(body.IsActive);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidToken_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LogoutResponse>();
        Assert.NotNull(body);
        Assert.Contains("Logged out", body.Msg);
    }

    [Fact]
    public async Task ForgotPassword_ValidEmail_ReturnsOk()
    {
        var client = Factory.CreateClient();

        var request = new ForgotPasswordRequest
        {
            Email = "testadmin@test.com"
        };

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ForgotPasswordResponse>();
        Assert.NotNull(body);
        Assert.True(body.Success);
    }

    // Response DTOs matching the API responses
    private record AuthMeResponse(
        int Id, string Name, string UserName, string Email,
        string? PhoneNumber, bool IsActive, DateTime CreatedAt,
        string Role, List<string> Roles,
        Dictionary<string, Dictionary<string, bool>> Permissions,
        bool MustChangePassword, bool TwoFactorEnabled);

    private record LogoutResponse(string Msg);

    private record RegisterResponse(string Message, int UserId);
}
