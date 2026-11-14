using System.Net.Http.Json;
using TradeLedger.Api.Contracts;
using TradeLedger.Api.Domain.Enums;
using TradeLedger.Api.Tests.TestFixtures;
using Xunit;

namespace TradeLedger.Api.Tests;

public class PortfolioTests : IDisposable
{
    private readonly TradeLedgerWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Positions_reflect_open_lots_after_a_buy()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);

        await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Market, 10, null));

        var positions = await client.GetFromJsonAsync<List<PositionResponse>>("/api/accounts/me/positions");

        var position = Assert.Single(positions!);
        Assert.Equal("AAPL", position.Symbol);
        Assert.Equal(10, position.Quantity);
        Assert.Equal(195.50m, position.AverageCost);
    }

    [Fact]
    public async Task Sell_spanning_two_lots_computes_fifo_realized_pnl()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);
        var (adminClient, _) = await AuthTestHelper.LoginAsAdminAsync(_factory);

        // Lot 1: 10 shares @ 195.50 (seeded price).
        await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Market, 10, null));

        // Lot 2: 10 shares @ 220.00 after an admin price update.
        await adminClient.PutAsJsonAsync("/api/securities/AAPL/price", new UpdatePriceRequest(220.00m));
        await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Market, 10, null));

        // Sell 15 @ 220.00: consumes all 10 of lot 1 (cost 1,955) and 5 of lot 2 (cost 1,100).
        // Proceeds = 15 * 220 = 3,300. Realized P&L = 3,300 - 1,955 - 1,100 = 245.
        var sellResponse = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Sell, OrderType.Market, 15, null));
        sellResponse.EnsureSuccessStatusCode();

        var trades = await client.GetFromJsonAsync<PagedResponse<TradeResponse>>("/api/accounts/me/trades");
        var sellTrade = Assert.Single(trades!.Items, t => t.Side == "Sell");
        Assert.Equal(245.00m, sellTrade.RealizedPnL);

        var positions = await client.GetFromJsonAsync<List<PositionResponse>>("/api/accounts/me/positions");
        var remaining = Assert.Single(positions!);
        Assert.Equal(5, remaining.Quantity);
        Assert.Equal(220.00m, remaining.AverageCost);
    }

    [Fact]
    public async Task Trades_are_paginated()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);
        for (var i = 0; i < 3; i++)
            await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Market, 1, null));

        var page1 = await client.GetFromJsonAsync<PagedResponse<TradeResponse>>("/api/accounts/me/trades?page=1&pageSize=2");

        Assert.Equal(2, page1!.Items.Count);
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
    }

    [Fact]
    public async Task Account_summary_reports_starting_cash_for_a_fresh_investor()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);

        var account = await client.GetFromJsonAsync<AccountSummaryResponse>("/api/accounts/me");

        Assert.Equal(10_000m, account!.CashBalance);
        Assert.Equal(10_000m, account.AvailableCash);
    }
}
