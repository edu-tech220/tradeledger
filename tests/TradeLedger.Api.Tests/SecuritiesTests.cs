using System.Net;
using System.Net.Http.Json;
using TradeLedger.Api.Contracts;
using TradeLedger.Api.Tests.TestFixtures;
using Xunit;

namespace TradeLedger.Api.Tests;

public class SecuritiesTests : IDisposable
{
    private readonly TradeLedgerWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetAll_returns_seeded_securities_without_authentication()
    {
        var client = _factory.CreateClient();

        var securities = await client.GetFromJsonAsync<List<SecurityResponse>>("/api/securities");

        Assert.NotNull(securities);
        Assert.Contains(securities!, s => s.Symbol == "AAPL");
    }

    [Fact]
    public async Task GetBySymbol_unknown_symbol_returns_not_found()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/securities/NOPE");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePrice_as_investor_is_forbidden()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);

        var response = await client.PutAsJsonAsync("/api/securities/AAPL/price", new UpdatePriceRequest(999.99m));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePrice_as_admin_updates_and_is_reflected_on_read()
    {
        var (client, _) = await AuthTestHelper.LoginAsAdminAsync(_factory);

        var updateResponse = await client.PutAsJsonAsync("/api/securities/AAPL/price", new UpdatePriceRequest(321.00m));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var security = await client.GetFromJsonAsync<SecurityResponse>("/api/securities/AAPL");
        Assert.Equal(321.00m, security!.CurrentPrice);
    }

    [Fact]
    public async Task UpdatePrice_without_authentication_is_unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/securities/AAPL/price", new UpdatePriceRequest(1m));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
