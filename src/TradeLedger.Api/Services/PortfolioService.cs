using Microsoft.EntityFrameworkCore;
using TradeLedger.Api.Common;
using TradeLedger.Api.Data;
using TradeLedger.Api.Domain.Entities;
using TradeLedger.Api.Domain.Exceptions;

namespace TradeLedger.Api.Services;

public class PortfolioService(IDbContextFactory<TradeLedgerDbContext> dbFactory) : IPortfolioService
{
    public async Task<Account> GetAccountAsync(int accountId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId, ct)
            ?? throw new EntityNotFoundException("Account", accountId);
    }

    public async Task<decimal> GetAvailableCashAsync(int accountId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await BuyingPower.GetAvailableCashAsync(db, accountId, ct);
    }

    public async Task<IReadOnlyList<PositionSummary>> GetPositionsAsync(int accountId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var openLots = await db.Lots.AsNoTracking()
            .Include(l => l.Security)
            .Where(l => l.AccountId == accountId && l.RemainingQuantity > 0)
            .ToListAsync(ct);

        return openLots
            .GroupBy(l => l.Security)
            .Select(group =>
            {
                var quantity = group.Sum(l => l.RemainingQuantity);
                var totalCost = group.Sum(l => l.RemainingQuantity * l.UnitCost);
                var averageCost = quantity == 0 ? 0m : totalCost / quantity;
                var marketValue = quantity * group.Key.CurrentPrice;

                return new PositionSummary(
                    group.Key.Symbol,
                    group.Key.Name,
                    quantity,
                    averageCost,
                    group.Key.CurrentPrice,
                    marketValue,
                    marketValue - totalCost);
            })
            .OrderBy(p => p.Symbol)
            .ToList();
    }

    public async Task<PagedResult<Trade>> GetTradesAsync(int accountId, int page, int pageSize, CancellationToken ct = default)
    {
        var (normalizedPage, normalizedPageSize) = Paging.Normalize(page, pageSize);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var query = db.Trades.AsNoTracking().Include(t => t.Security).Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.ExecutedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).ToListAsync(ct);

        return new PagedResult<Trade>
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = totalCount,
        };
    }
}
