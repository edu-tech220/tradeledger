using Microsoft.EntityFrameworkCore;
using TradeLedger.Api.Common;
using TradeLedger.Api.Data;
using TradeLedger.Api.Domain.Entities;
using TradeLedger.Api.Domain.Enums;
using TradeLedger.Api.Domain.Exceptions;

namespace TradeLedger.Api.Services;

public class OrderService(IDbContextFactory<TradeLedgerDbContext> dbFactory) : IOrderService
{
    private const int MaxConcurrencyAttempts = 5;

    private enum FillOutcome
    {
        NotApplicable,
        Filled,
        InsufficientFunds,
        InsufficientHoldings,
    }

    public async Task<Order> PlaceOrderAsync(
        int accountId,
        string symbol,
        OrderSide side,
        OrderType type,
        int quantity,
        decimal? limitPrice,
        CancellationToken ct = default)
    {
        if (quantity <= 0)
            throw new InvalidOrderRequestException("Quantity must be greater than zero.");

        if (type == OrderType.Limit && (limitPrice is null || limitPrice <= 0))
            throw new InvalidOrderRequestException("Limit orders require a positive limit price.");

        if (type == OrderType.Market && limitPrice is not null)
            throw new InvalidOrderRequestException("Market orders must not specify a limit price.");

        int orderId;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var security = await db.Securities.FirstOrDefaultAsync(s => s.Symbol == symbol, ct)
                ?? throw new EntityNotFoundException("Security", symbol);

            _ = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, ct)
                ?? throw new EntityNotFoundException("Account", accountId);

            if (side == OrderSide.Buy)
            {
                var cost = quantity * (type == OrderType.Market ? security.CurrentPrice : limitPrice!.Value);
                var availableCash = await BuyingPower.GetAvailableCashAsync(db, accountId, ct);
                if (cost > availableCash)
                    throw new InsufficientFundsException(cost, availableCash);
            }
            else
            {
                var availableQuantity = await BuyingPower.GetAvailableQuantityAsync(db, accountId, security.Id, ct);
                if (quantity > availableQuantity)
                    throw new InsufficientHoldingsException(symbol, quantity, availableQuantity);
            }

            var order = new Order
            {
                AccountId = accountId,
                SecurityId = security.Id,
                Side = side,
                Type = type,
                LimitPrice = limitPrice,
                Quantity = quantity,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
            orderId = order.Id;
        }

        // Market orders always cross immediately; limit orders cross only if the
        // current price already satisfies the limit, otherwise they stay Pending
        // until a price update (or the background sweep) makes them fillable.
        await TryFillWithRetryAsync(orderId, ct);

        return await GetOrderAsync(accountId, orderId, ct);
    }

    public async Task<Order> CancelOrderAsync(int accountId, int orderId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var order = await db.Orders.Include(o => o.Security)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.AccountId == accountId, ct)
            ?? throw new EntityNotFoundException("Order", orderId);

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOrderStateException(orderId, order.Status.ToString());

        order.Status = OrderStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return order;
    }

    public async Task<Order> GetOrderAsync(int accountId, int orderId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Orders.Include(o => o.Security).AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.AccountId == accountId, ct)
            ?? throw new EntityNotFoundException("Order", orderId);
    }

    public async Task<PagedResult<Order>> GetOrdersAsync(
        int accountId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var (normalizedPage, normalizedPageSize) = Paging.Normalize(page, pageSize);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var query = db.Orders.AsNoTracking().Include(o => o.Security).Where(o => o.AccountId == accountId);
        if (status is not null)
            query = query.Where(o => o.Status == status);
        query = query.OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).ToListAsync(ct);

        return new PagedResult<Order>
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<int> MatchPendingOrdersAsync(CancellationToken ct = default)
    {
        List<int> pendingLimitOrderIds;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            pendingLimitOrderIds = await db.Orders.AsNoTracking()
                .Where(o => o.Status == OrderStatus.Pending && o.Type == OrderType.Limit)
                .OrderBy(o => o.CreatedAt)
                .Select(o => o.Id)
                .ToListAsync(ct);
        }

        var filledCount = 0;
        foreach (var orderId in pendingLimitOrderIds)
        {
            var outcome = await TryFillWithRetryAsync(orderId, ct);
            if (outcome == FillOutcome.Filled)
                filledCount++;
        }

        return filledCount;
    }

    private async Task<FillOutcome> TryFillWithRetryAsync(int orderId, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyAttempts; attempt++)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            try
            {
                return await AttemptFillAsync(db, orderId, ct);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyAttempts)
            {
                // Another request mutated the account or its lots in between; the next
                // attempt opens a brand new DbContext and reads a fresh snapshot.
            }
        }

        throw new ConcurrencyRetryExhaustedException($"fill order {orderId}");
    }

    private static async Task<FillOutcome> AttemptFillAsync(TradeLedgerDbContext db, int orderId, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.Status == OrderStatus.Pending, ct);
        if (order is null)
            return FillOutcome.NotApplicable;

        var security = await db.Securities.FirstAsync(s => s.Id == order.SecurityId, ct);

        var crosses = order.Type == OrderType.Market
            || (order.Side == OrderSide.Buy && security.CurrentPrice <= order.LimitPrice)
            || (order.Side == OrderSide.Sell && security.CurrentPrice >= order.LimitPrice);
        if (!crosses)
            return FillOutcome.NotApplicable;

        var account = await db.Accounts.FirstAsync(a => a.Id == order.AccountId, ct);
        var fillPrice = security.CurrentPrice;

        if (order.Side == OrderSide.Buy)
        {
            var cost = fillPrice * order.Quantity;
            if (cost > account.CashBalance)
                return FillOutcome.InsufficientFunds;

            account.CashBalance -= cost;
            account.Version++;

            db.Lots.Add(new Lot
            {
                AccountId = order.AccountId,
                SecurityId = order.SecurityId,
                BuyOrderId = order.Id,
                OriginalQuantity = order.Quantity,
                RemainingQuantity = order.Quantity,
                UnitCost = fillPrice,
                PurchasedAt = DateTime.UtcNow,
            });

            db.Trades.Add(new Trade
            {
                OrderId = order.Id,
                AccountId = order.AccountId,
                SecurityId = order.SecurityId,
                Side = OrderSide.Buy,
                Quantity = order.Quantity,
                Price = fillPrice,
                RealizedPnL = null,
                ExecutedAt = DateTime.UtcNow,
            });
        }
        else
        {
            var lots = await db.Lots
                .Where(l => l.AccountId == order.AccountId && l.SecurityId == order.SecurityId && l.RemainingQuantity > 0)
                .OrderBy(l => l.PurchasedAt)
                .ToListAsync(ct);

            if (lots.Sum(l => l.RemainingQuantity) < order.Quantity)
                return FillOutcome.InsufficientHoldings;

            var remainingToSell = order.Quantity;
            var costBasis = 0m;
            foreach (var lot in lots)
            {
                if (remainingToSell <= 0)
                    break;

                var take = Math.Min(lot.RemainingQuantity, remainingToSell);
                lot.RemainingQuantity -= take;
                lot.Version++;
                costBasis += take * lot.UnitCost;
                remainingToSell -= take;
            }

            var proceeds = fillPrice * order.Quantity;
            account.CashBalance += proceeds;
            account.Version++;

            db.Trades.Add(new Trade
            {
                OrderId = order.Id,
                AccountId = order.AccountId,
                SecurityId = order.SecurityId,
                Side = OrderSide.Sell,
                Quantity = order.Quantity,
                Price = fillPrice,
                RealizedPnL = proceeds - costBasis,
                ExecutedAt = DateTime.UtcNow,
            });
        }

        order.Status = OrderStatus.Filled;
        order.FilledAt = DateTime.UtcNow;
        order.FilledPrice = fillPrice;

        await db.SaveChangesAsync(ct);
        return FillOutcome.Filled;
    }
}
