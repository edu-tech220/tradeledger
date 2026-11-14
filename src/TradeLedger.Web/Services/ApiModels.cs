namespace TradeLedger.Web.Services;

public record AuthResponse(string Token, DateTime ExpiresAt, string Username, string Role, int AccountId);

public record AccountSummaryResponse(int AccountId, decimal CashBalance, decimal AvailableCash);

public record PositionResponse(
    string Symbol,
    string Name,
    int Quantity,
    decimal AverageCost,
    decimal CurrentPrice,
    decimal MarketValue,
    decimal UnrealizedPnL);

public record TradeResponse(int Id, string Symbol, string Side, int Quantity, decimal Price, decimal? RealizedPnL, DateTime ExecutedAt);

public record SecurityResponse(string Symbol, string Name, decimal CurrentPrice, DateTime UpdatedAt);

public record OrderResponse(
    int Id,
    string Symbol,
    string Side,
    string Type,
    decimal? LimitPrice,
    int Quantity,
    string Status,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? FilledAt,
    decimal? FilledPrice);

public record AdminAccountResponse(int AccountId, string Username, string Role, decimal CashBalance);

public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public record ApiProblem(string? Title, string? Detail, int? Status);

public class ApiException(string message, ApiProblem? problem) : Exception(message)
{
    public ApiProblem? Problem { get; } = problem;
}
