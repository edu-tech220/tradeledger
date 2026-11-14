using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeLedger.Api.Common;
using TradeLedger.Api.Contracts;
using TradeLedger.Api.Data;

namespace TradeLedger.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(IDbContextFactory<TradeLedgerDbContext> dbFactory) : ControllerBase
{
    [HttpGet("accounts")]
    public async Task<ActionResult<PagedResponse<AdminAccountResponse>>> GetAccounts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (normalizedPage, normalizedPageSize) = Paging.Normalize(page, pageSize);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var query = db.Accounts.AsNoTracking().Include(a => a.User).OrderBy(a => a.User.Username);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(a => new AdminAccountResponse(a.Id, a.User.Username, a.User.Role.ToString(), a.CashBalance))
            .ToListAsync(ct);

        var totalPages = normalizedPageSize == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);
        return Ok(new PagedResponse<AdminAccountResponse>(items, normalizedPage, normalizedPageSize, totalCount, totalPages));
    }
}
