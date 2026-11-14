using TradeLedger.Api.Domain.Entities;
using TradeLedger.Api.Domain.Enums;
using TradeLedger.Api.Infrastructure.Security;

namespace TradeLedger.Api.Data;

public static class DbSeeder
{
    public const string DemoAdminUsername = "admin";
    public const string DemoAdminPassword = "Admin#12345";
    public const string DemoInvestorUsername = "investor";
    public const string DemoInvestorPassword = "Investor#12345";

    public static async Task SeedAsync(TradeLedgerDbContext db)
    {
        if (!db.Securities.Any())
        {
            var now = DateTime.UtcNow;
            db.Securities.AddRange(
                new Security { Symbol = "AAPL", Name = "Apple Inc.", CurrentPrice = 195.50m, UpdatedAt = now },
                new Security { Symbol = "MSFT", Name = "Microsoft Corp.", CurrentPrice = 410.25m, UpdatedAt = now },
                new Security { Symbol = "GOOGL", Name = "Alphabet Inc.", CurrentPrice = 165.80m, UpdatedAt = now },
                new Security { Symbol = "TSLA", Name = "Tesla Inc.", CurrentPrice = 245.10m, UpdatedAt = now },
                new Security { Symbol = "AMZN", Name = "Amazon.com Inc.", CurrentPrice = 178.35m, UpdatedAt = now }
            );
        }

        if (!db.Users.Any(u => u.Username == DemoAdminUsername))
        {
            var (hash, salt) = PasswordHasher.Hash(DemoAdminPassword);
            db.Users.Add(new AppUser
            {
                Username = DemoAdminUsername,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                Account = new Account { CashBalance = 0m },
            });
        }

        if (!db.Users.Any(u => u.Username == DemoInvestorUsername))
        {
            var (hash, salt) = PasswordHasher.Hash(DemoInvestorPassword);
            db.Users.Add(new AppUser
            {
                Username = DemoInvestorUsername,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = UserRole.Investor,
                CreatedAt = DateTime.UtcNow,
                Account = new Account { CashBalance = 100_000m },
            });
        }

        await db.SaveChangesAsync();
    }
}
