using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TradeLedger.Api.Data;

namespace TradeLedger.Api.Tests.TestFixtures;

/// <summary>
/// Boots the real API through WebApplicationFactory but swaps the SQL Server DbContext for an
/// isolated, in-memory SQLite database per instance, so every test class gets a clean schema
/// without needing a real SQL Server instance in CI.
/// </summary>
public class TradeLedgerWebApplicationFactory : WebApplicationFactory<Program>
{
    // EF Core resolves IDbContextFactory as a singleton and the order-matching retry loop relies
    // on it handing out a brand new connection per attempt, so the DbContext must be configured
    // with a connection STRING, not one shared SqliteConnection instance - a single ADO.NET
    // connection object cannot safely run concurrent commands from multiple threads/contexts.
    // This field only exists to keep *a* connection open so the in-memory database itself
    // survives between those short-lived connections; it is never handed to the DbContext.
    private readonly SqliteConnection _keepAliveConnection;
    private readonly string _connectionString;

    public TradeLedgerWebApplicationFactory()
    {
        // Default Timeout makes SQLite wait (up to 30s) for another connection's write lock to
        // free instead of throwing SQLITE_BUSY immediately - needed because SQLite only allows
        // one writer at a time even across separate connections to the same shared-cache database.
        _connectionString = $"DataSource=test-{Guid.NewGuid()};Mode=Memory;Cache=Shared;Default Timeout=30";
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TradeLedgerDbContext>>();
            services.RemoveAll<IDbContextFactory<TradeLedgerDbContext>>();

            services.AddDbContextFactory<TradeLedgerDbContext>(options => options.UseSqlite(_connectionString));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _keepAliveConnection.Dispose();
    }
}
