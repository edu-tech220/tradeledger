using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TradeLedger.Api.Contracts;
using TradeLedger.Api.Domain.Enums;
using TradeLedger.Api.Services;
using TradeLedger.Api.Tests.TestFixtures;
using Xunit;

namespace TradeLedger.Api.Tests;

/// <summary>
/// The matching sweep is also driven by OrderMatchingBackgroundService on a timer, but these
/// tests call IOrderService.MatchPendingOrdersAsync() directly through the app's own DI
/// container for a deterministic, timer-free check of the same logic.
/// </summary>
public class MatchingTests : IDisposable
{
    private readonly TradeLedgerWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Pending_limit_buy_fills_once_price_drops_to_the_limit()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);
        var (adminClient, _) = await AuthTestHelper.LoginAsAdminAsync(_factory);

        var placeResponse = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("TSLA", OrderSide.Buy, OrderType.Limit, 5, 150m));
        var order = await placeResponse.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal("Pending", order!.Status);

        await adminClient.PutAsJsonAsync("/api/securities/TSLA/price", new UpdatePriceRequest(140m));

        using var scope = _factory.Services.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var filledCount = await orderService.MatchPendingOrdersAsync();
        Assert.Equal(1, filledCount);

        var refreshed = await client.GetFromJsonAsync<OrderResponse>($"/api/orders/{order.Id}");
        Assert.Equal("Filled", refreshed!.Status);
        Assert.Equal(140m, refreshed.FilledPrice);
    }

    [Fact]
    public async Task Pending_limit_sell_fills_once_price_rises_to_the_limit()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);
        var (adminClient, _) = await AuthTestHelper.LoginAsAdminAsync(_factory);

        await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("TSLA", OrderSide.Buy, OrderType.Market, 5, null));
        var placeResponse = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("TSLA", OrderSide.Sell, OrderType.Limit, 5, 300m));
        var order = await placeResponse.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal("Pending", order!.Status);

        await adminClient.PutAsJsonAsync("/api/securities/TSLA/price", new UpdatePriceRequest(310m));

        using var scope = _factory.Services.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        await orderService.MatchPendingOrdersAsync();

        var refreshed = await client.GetFromJsonAsync<OrderResponse>($"/api/orders/{order.Id}");
        Assert.Equal("Filled", refreshed!.Status);
    }

    [Fact]
    public async Task Sweep_with_no_eligible_orders_fills_nothing()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);
        await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("TSLA", OrderSide.Buy, OrderType.Limit, 5, 1m));

        using var scope = _factory.Services.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var filledCount = await orderService.MatchPendingOrdersAsync();

        Assert.Equal(0, filledCount);
    }
}
