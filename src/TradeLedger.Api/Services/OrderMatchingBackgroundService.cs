namespace TradeLedger.Api.Services;

/// <summary>
/// Safety net for limit orders: PlaceOrderAsync already tries to fill a limit order the
/// moment it is created, but an order can stay Pending if the market hasn't crossed its
/// limit yet. This sweep periodically re-checks every Pending limit order so that it still
/// fills once an admin price update makes it eligible, even without a new request arriving.
/// </summary>
public class OrderMatchingBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderMatchingBackgroundService> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = configuration.GetValue("OrderMatching:SweepIntervalSeconds", 5);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                var filled = await orderService.MatchPendingOrdersAsync(stoppingToken);
                if (filled > 0)
                    logger.LogInformation("Order matching sweep filled {Count} pending limit order(s)", filled);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Order matching sweep failed");
            }
        }
    }
}
