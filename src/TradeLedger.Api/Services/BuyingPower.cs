using Microsoft.EntityFrameworkCore;
using TradeLedger.Api.Data;
using TradeLedger.Api.Domain.Enums;

namespace TradeLedger.Api.Services;

/// <summary>
/// Cash and share quantities actually free to trade with: the account/lot balances
/// minus whatever is already reserved by other pending limit orders.
/// </summary>
public static class BuyingPower
{
    public static async Task<decimal> GetAvailableCashAsync(TradeLedgerDbContext db, int accountId, CancellationToken ct = default)
    {
        var account = await db.Accounts.AsNoTracking().FirstAsync(a => a.Id == accountId, ct);

        // Summed client-side: the SQLite provider used in tests cannot translate
        // Sum() over decimal expressions, and the pending-order set per account is small.
        var pendingBuys = await db.Orders.AsNoTracking()
            .Where(o => o.AccountId == accountId && o.Status == OrderStatus.Pending && o.Side == OrderSide.Buy)
            .Select(o => new { o.Quantity, o.Type, o.LimitPrice })
            .ToListAsync(ct);

        var reserved = pendingBuys.Sum(o => o.Quantity * (o.Type == OrderType.Market ? 0m : o.LimitPrice!.Value));

        return account.CashBalance - reserved;
    }

    public static async Task<int> GetAvailableQuantityAsync(TradeLedgerDbContext db, int accountId, int securityId, CancellationToken ct = default)
    {
        var held = await db.Lots.AsNoTracking()
            .Where(l => l.AccountId == accountId && l.SecurityId == securityId)
            .SumAsync(l => l.RemainingQuantity, ct);

        var reserved = await db.Orders.AsNoTracking()
            .Where(o => o.AccountId == accountId && o.SecurityId == securityId && o.Status == OrderStatus.Pending && o.Side == OrderSide.Sell)
            .SumAsync(o => o.Quantity, ct);

        return held - reserved;
    }
}
