using System.Net;
using System.Net.Http.Json;
using TradeLedger.Api.Contracts;
using TradeLedger.Api.Domain.Enums;
using TradeLedger.Api.Tests.TestFixtures;
using Xunit;

namespace TradeLedger.Api.Tests;

public class OrdersTests : IDisposable
{
    private readonly TradeLedgerWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Market_buy_order_fills_immediately_and_deducts_cash()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Market, 10, null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal("Filled", order!.Status);
        Assert.NotNull(order.FilledPrice);

        var account = await client.GetFromJsonAsync<AccountSummaryResponse>("/api/accounts/me");
        Assert.Equal(10_000m - order.FilledPrice!.Value * 10, account!.CashBalance);
    }

    [Fact]
    public async Task Market_buy_exceeding_cash_balance_is_rejected_with_422()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);

        // AAPL seeds at 195.50; 10,000 cash cannot buy 1,000 shares.
        var response = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Market, 1_000, null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Market_sell_without_holdings_is_rejected_with_422()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Sell, OrderType.Market, 1, null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Zero_quantity_order_is_rejected_with_400()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Market, 0, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Limit_buy_above_market_fills_immediately()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);

        // AAPL seeds at 195.50; a limit far above that should cross immediately.
        var response = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Limit, 5, 500m));
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();

        Assert.Equal("Filled", order!.Status);
        Assert.Equal(195.50m, order.FilledPrice);
    }

    [Fact]
    public async Task Limit_buy_below_market_stays_pending()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Limit, 5, 50m));
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();

        Assert.Equal("Pending", order!.Status);
    }

    [Fact]
    public async Task Pending_limit_order_reserves_cash_from_available_balance()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);

        await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Limit, 10, 50m));

        var account = await client.GetFromJsonAsync<AccountSummaryResponse>("/api/accounts/me");
        Assert.Equal(10_000m, account!.CashBalance);
        Assert.Equal(9_500m, account.AvailableCash);
    }

    [Fact]
    public async Task Cancel_pending_order_releases_the_reservation()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);
        var placeResponse = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Limit, 10, 50m));
        var order = await placeResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var cancelResponse = await client.PostAsync($"/api/orders/{order!.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal("Cancelled", cancelled!.Status);

        var account = await client.GetFromJsonAsync<AccountSummaryResponse>("/api/accounts/me");
        Assert.Equal(10_000m, account!.AvailableCash);
    }

    [Fact]
    public async Task Cancel_already_filled_order_returns_conflict()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);
        var placeResponse = await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Market, 1, null));
        var order = await placeResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var cancelResponse = await client.PostAsync($"/api/orders/{order!.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.Conflict, cancelResponse.StatusCode);
    }

    [Fact]
    public async Task GetOrder_for_another_accounts_order_returns_not_found()
    {
        var (clientA, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);
        var placeResponse = await clientA.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Market, 1, null));
        var order = await placeResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var (clientB, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);
        var response = await clientB.GetAsync($"/api/orders/{order!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_filters_by_status()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);
        await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Market, 1, null));
        await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("MSFT", OrderSide.Buy, OrderType.Limit, 1, 50m));

        var pending = await client.GetFromJsonAsync<PagedResponse<OrderResponse>>("/api/orders?status=Pending");
        var filled = await client.GetFromJsonAsync<PagedResponse<OrderResponse>>("/api/orders?status=Filled");

        Assert.Single(pending!.Items);
        Assert.Single(filled!.Items);
    }

    [Fact]
    public async Task GetOrders_paginates_results()
    {
        var (client, _) = await AuthTestHelper.RegisterInvestorAsync(_factory);
        for (var i = 0; i < 5; i++)
            await client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest("AAPL", OrderSide.Buy, OrderType.Market, 1, null));

        var page1 = await client.GetFromJsonAsync<PagedResponse<OrderResponse>>("/api/orders?page=1&pageSize=2");
        var page2 = await client.GetFromJsonAsync<PagedResponse<OrderResponse>>("/api/orders?page=2&pageSize=2");

        Assert.Equal(2, page1!.Items.Count);
        Assert.Equal(2, page2!.Items.Count);
        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
        Assert.NotEqual(page1.Items[0].Id, page2.Items[0].Id);
    }
}
