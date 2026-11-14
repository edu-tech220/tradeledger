using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeLedger.Api.Contracts;
using TradeLedger.Api.Data;

namespace TradeLedger.Api.Controllers;

[ApiController]
[Route("api/securities")]
public class SecuritiesController(IDbContextFactory<TradeLedgerDbContext> dbFactory) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SecurityResponse>>> GetAll(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var securities = await db.Securities.AsNoTracking().OrderBy(s => s.Symbol).ToListAsync(ct);
        return Ok(securities.Select(s => new SecurityResponse(s.Symbol, s.Name, s.CurrentPrice, s.UpdatedAt)).ToList());
    }

    [HttpGet("{symbol}")]
    public async Task<ActionResult<SecurityResponse>> GetBySymbol(string symbol, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var security = await db.Securities.AsNoTracking().FirstOrDefaultAsync(s => s.Symbol == symbol.ToUpperInvariant(), ct);
        if (security is null)
            return NotFound();

        return Ok(new SecurityResponse(security.Symbol, security.Name, security.CurrentPrice, security.UpdatedAt));
    }

    [HttpPut("{symbol}/price")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SecurityResponse>> UpdatePrice(string symbol, UpdatePriceRequest request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var security = await db.Securities.FirstOrDefaultAsync(s => s.Symbol == symbol.ToUpperInvariant(), ct);
        if (security is null)
            return NotFound();

        security.CurrentPrice = request.Price;
        security.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(new SecurityResponse(security.Symbol, security.Name, security.CurrentPrice, security.UpdatedAt));
    }
}
