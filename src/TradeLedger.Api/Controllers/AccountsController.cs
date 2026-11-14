using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeLedger.Api.Contracts;
using TradeLedger.Api.Infrastructure.Security;
using TradeLedger.Api.Services;

namespace TradeLedger.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public class AccountsController(IPortfolioService portfolioService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<AccountSummaryResponse>> GetMe(CancellationToken ct)
    {
        var accountId = User.GetAccountId();
        var account = await portfolioService.GetAccountAsync(accountId, ct);
        var availableCash = await portfolioService.GetAvailableCashAsync(accountId, ct);
        return Ok(new AccountSummaryResponse(account.Id, account.CashBalance, availableCash));
    }

    [HttpGet("me/positions")]
    public async Task<ActionResult<IReadOnlyList<PositionResponse>>> GetPositions(CancellationToken ct)
    {
        var accountId = User.GetAccountId();
        var positions = await portfolioService.GetPositionsAsync(accountId, ct);
        return Ok(positions
            .Select(p => new PositionResponse(p.Symbol, p.Name, p.Quantity, p.AverageCost, p.CurrentPrice, p.MarketValue, p.UnrealizedPnL))
            .ToList());
    }

    [HttpGet("me/trades")]
    public async Task<ActionResult<PagedResponse<TradeResponse>>> GetTrades(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var accountId = User.GetAccountId();
        var result = await portfolioService.GetTradesAsync(accountId, page, pageSize, ct);
        var items = result.Items
            .Select(t => new TradeResponse(t.Id, t.Security.Symbol, t.Side.ToString(), t.Quantity, t.Price, t.RealizedPnL, t.ExecutedAt))
            .ToList();

        return Ok(new PagedResponse<TradeResponse>(items, result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }
}
