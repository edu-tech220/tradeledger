using System.Net;
using System.Net.Http.Json;
using TradeLedger.Api.Contracts;
using TradeLedger.Api.Tests.TestFixtures;
using Xunit;

namespace TradeLedger.Api.Tests;

public class AdminTests : IDisposable
{
    private readonly TradeLedgerWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Investor_cannot_list_all_accounts()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);

        var response = await client.GetAsync("/api/admin/accounts");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_list_all_accounts_including_seeded_demo_users()
    {
        var (client, _) = await AuthTestHelper.LoginAsAdminAsync(_factory);
        await AuthTestHelper.RegisterInvestorAsync(_factory, "another-investor");

        var result = await client.GetFromJsonAsync<PagedResponse<AdminAccountResponse>>("/api/admin/accounts?pageSize=50");

        Assert.NotNull(result);
        Assert.Contains(result!.Items, a => a.Username == "admin");
        Assert.Contains(result.Items, a => a.Username == "investor");
        Assert.Contains(result.Items, a => a.Username == "another-investor");
    }
}
