using TradeLedger.Api.Common;
using TradeLedger.Api.Domain.Entities;
using TradeLedger.Api.Domain.Enums;

namespace TradeLedger.Api.Services;

public interface IOrderService
{
    Task<Order> PlaceOrderAsync(
        int accountId,
        string symbol,
        OrderSide side,
        OrderType type,
        int quantity,
        decimal? limitPrice,
        CancellationToken ct = default);

    Task<Order> CancelOrderAsync(int accountId, int orderId, CancellationToken ct = default);

    Task<PagedResult<Order>> GetOrdersAsync(
        int accountId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Order> GetOrderAsync(int accountId, int orderId, CancellationToken ct = default);

    /// <summary>Re-evaluates every pending limit order against current market prices. Returns how many filled.</summary>
    Task<int> MatchPendingOrdersAsync(CancellationToken ct = default);
}
