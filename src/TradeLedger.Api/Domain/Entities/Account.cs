namespace TradeLedger.Api.Domain.Entities;

public class Account
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal CashBalance { get; set; }

    // Application-managed concurrency token (not a DB-generated rowversion) so the
    // same optimistic-concurrency code path works identically on SQL Server and SQLite.
    public uint Version { get; set; }

    public AppUser User { get; set; } = null!;
    public List<Order> Orders { get; set; } = [];
    public List<Lot> Lots { get; set; } = [];
    public List<Trade> Trades { get; set; } = [];
}
