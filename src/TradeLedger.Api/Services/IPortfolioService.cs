using TradeLedger.Api.Common;
using TradeLedger.Api.Domain.Entities;

namespace TradeLedger.Api.Services;

public interface IPortfolioService
{
    Task<Account> GetAccountAsync(int accountId, CancellationToken ct = default);

    Task<decimal> GetAvailableCashAsync(int accountId, CancellationToken ct = default);

    Task<IReadOnlyList<PositionSummary>> GetPositionsAsync(int accountId, CancellationToken ct = default);

    Task<PagedResult<Trade>> GetTradesAsync(int accountId, int page, int pageSize, CancellationToken ct = default);
}
