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
    private readonly SqliteConnection _connection;

    public TradeLedgerWebApplicationFactory()
    {
        _connection = new SqliteConnection($"DataSource=test-{Guid.NewGuid()};Mode=Memory;Cache=Shared");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TradeLedgerDbContext>>();
            services.RemoveAll<IDbContextFactory<TradeLedgerDbContext>>();

            services.AddDbContextFactory<TradeLedgerDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
