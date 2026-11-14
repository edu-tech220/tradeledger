using System.Net.Http.Json;
using TradeLedger.Api.Contracts;
using TradeLedger.Api.Domain.Enums;
using TradeLedger.Api.Tests.TestFixtures;
using Xunit;

namespace TradeLedger.Api.Tests;

public class ConcurrencyTests : IDisposable
{
    private readonly TradeLedgerWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Concurrent_buy_orders_never_let_cash_balance_go_negative()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);

        // AAPL seeds at 195.50; 10,000 cash affords ~51 shares. Fire 10 concurrent orders for
        // 10 shares each (would need ~19,550 total) so some must be rejected for insufficient
        // funds while the concurrency retry loop keeps the account balance consistent.
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Market, 10, null)));
        var responses = await Task.WhenAll(tasks);

        var filledCount = 0;
        foreach (var response in responses)
        {
            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
                if (order!.Status == "Filled")
                    filledCount++;
            }
        }

        var account = await client.GetFromJsonAsync<AccountSummaryResponse>("/api/accounts/me");
        Assert.True(account!.CashBalance >= 0, "Cash balance must never go negative.");
        Assert.Equal(10_000m - filledCount * 10 * 195.50m, account.CashBalance);

        var positions = await client.GetFromJsonAsync<List<PositionResponse>>("/api/accounts/me/positions");
        var totalShares = positions!.Sum(p => p.Quantity);
        Assert.Equal(filledCount * 10, totalShares);
    }

    [Fact]
    public async Task Concurrent_sell_orders_never_oversell_the_same_lot()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);
        await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Market, 10, null));

        // Only 10 shares are held; fire 5 concurrent sell orders for 5 shares each (needs 25
        // total), so at most 2 can succeed.
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Sell, OrderType.Market, 5, null)));
        var responses = await Task.WhenAll(tasks);

        var filledCount = 0;
        foreach (var response in responses)
        {
            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
                if (order!.Status == "Filled")
                    filledCount++;
            }
        }

        Assert.True(filledCount <= 2, "No more than 2 sell orders of 5 shares can fill against a 10-share holding.");

        var positions = await client.GetFromJsonAsync<List<PositionResponse>>("/api/accounts/me/positions");
        var remainingShares = positions!.Sum(p => p.Quantity);
        Assert.Equal(10 - filledCount * 5, remainingShares);
        Assert.True(remainingShares >= 0, "Remaining shares must never go negative.");
    }
}
