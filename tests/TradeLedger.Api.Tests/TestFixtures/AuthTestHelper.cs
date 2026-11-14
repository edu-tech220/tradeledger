using System.Net.Http.Headers;
using System.Net.Http.Json;
using TradeLedger.Api.Contracts;
using TradeLedger.Api.Data;

namespace TradeLedger.Api.Tests.TestFixtures;

public static class AuthTestHelper
{
    public static async Task<(HttpClient Client, AuthResponse Auth)> RegisterInvestorAsync(
        TradeLedgerWebApplicationFactory factory, string? username = null, string password = "Passw0rd#123")
    {
        var client = factory.CreateClient();
        username ??= $"user{Guid.NewGuid():N}"[..16];

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(username, password));
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth);
    }

    public static async Task<(HttpClient Client, AuthResponse Auth)> LoginAsAdminAsync(TradeLedgerWebApplicationFactory factory)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(DbSeeder.DemoAdminUsername, DbSeeder.DemoAdminPassword));
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth);
    }
}
