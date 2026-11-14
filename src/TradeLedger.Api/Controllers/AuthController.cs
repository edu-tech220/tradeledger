using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TradeLedger.Api.Contracts;
using TradeLedger.Api.Data;
using TradeLedger.Api.Domain.Entities;
using TradeLedger.Api.Domain.Enums;
using TradeLedger.Api.Infrastructure.Security;

namespace TradeLedger.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(IDbContextFactory<TradeLedgerDbContext> dbFactory, JwtTokenGenerator tokenGenerator) : ControllerBase
{
    private const decimal StartingCashBalance = 10_000m;

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        if (await db.Users.AnyAsync(u => u.Username == request.Username, ct))
            return Conflict(new ProblemDetails { Title = "Username already taken", Status = StatusCodes.Status409Conflict });

        var (hash, salt) = PasswordHasher.Hash(request.Password);
        var user = new AppUser
        {
            Username = request.Username,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.Investor,
            CreatedAt = DateTime.UtcNow,
            Account = new Account { CashBalance = StartingCashBalance },
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var (token, expiresAt) = tokenGenerator.GenerateToken(user);
        return Ok(new AuthResponse(token, expiresAt, user.Username, user.Role.ToString(), user.Account.Id));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var user = await db.Users.Include(u => u.Account).FirstOrDefaultAsync(u => u.Username == request.Username, ct);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
            return Unauthorized(new ProblemDetails { Title = "Invalid username or password", Status = StatusCodes.Status401Unauthorized });

        var (token, expiresAt) = tokenGenerator.GenerateToken(user);
        return Ok(new AuthResponse(token, expiresAt, user.Username, user.Role.ToString(), user.Account!.Id));
    }
}
