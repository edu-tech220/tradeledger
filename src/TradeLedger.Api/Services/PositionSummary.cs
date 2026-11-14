namespace TradeLedger.Api.Services;

public sealed record PositionSummary(
    string Symbol,
    string Name,
    int Quantity,
    decimal AverageCost,
    decimal CurrentPrice,
    decimal MarketValue,
    decimal UnrealizedPnL);
