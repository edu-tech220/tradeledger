using System.Net;
using System.Net.Http.Json;
using TradeLedger.Api.Contracts;
using TradeLedger.Api.Data;
using TradeLedger.Api.Tests.TestFixtures;
using Xunit;

namespace TradeLedger.Api.Tests;

public class AuthTests : IDisposable
{
    private readonly TradeLedgerWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Register_creates_investor_account_with_starting_cash()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("newinvestor", "Passw0rd#123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal("Investor", auth!.Role);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
    }

    [Fact]
    public async Task Register_with_duplicate_username_returns_conflict()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("dupeuser", "Passw0rd#123"));

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("dupeuser", "Different#123"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_with_short_password_fails_validation()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("shortpass", "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_correct_credentials_returns_token()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(DbSeeder.DemoInvestorUsername, DbSeeder.DemoInvestorPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(DbSeeder.DemoInvestorUsername, auth!.Username);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(DbSeeder.DemoInvestorUsername, "WrongPassword#1"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_unknown_username_returns_unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("doesnotexist", "Whatever#123"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Accounts_me_without_token_returns_unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/accounts/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
