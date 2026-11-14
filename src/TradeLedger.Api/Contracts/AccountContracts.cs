namespace TradeLedger.Api.Contracts;

public record AccountSummaryResponse(int AccountId, decimal CashBalance, decimal AvailableCash);

public record PositionResponse(
    string Symbol,
    string Name,
    int Quantity,
    decimal AverageCost,
    decimal CurrentPrice,
    decimal MarketValue,
    decimal UnrealizedPnL);

public record TradeResponse(
    int Id,
    string Symbol,
    string Side,
    int Quantity,
    decimal Price,
    decimal? RealizedPnL,
    DateTime ExecutedAt);
