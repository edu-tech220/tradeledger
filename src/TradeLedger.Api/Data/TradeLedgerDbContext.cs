using Microsoft.EntityFrameworkCore;
using TradeLedger.Api.Domain.Entities;

namespace TradeLedger.Api.Data;

public class TradeLedgerDbContext(DbContextOptions<TradeLedgerDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Security> Securities => Set<Security>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<Trade> Trades => Set<Trade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(64);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.Property(a => a.CashBalance).HasColumnType("decimal(18,2)");
            entity.Property(a => a.Version).IsConcurrencyToken();

            entity.HasOne(a => a.User)
                .WithOne(u => u.Account)
                .HasForeignKey<Account>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Security>(entity =>
        {
            entity.HasIndex(s => s.Symbol).IsUnique();
            entity.Property(s => s.Symbol).HasMaxLength(10);
            entity.Property(s => s.Name).HasMaxLength(128);
            entity.Property(s => s.CurrentPrice).HasColumnType("decimal(18,4)");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.LimitPrice).HasColumnType("decimal(18,4)");
            entity.Property(o => o.FilledPrice).HasColumnType("decimal(18,4)");
            entity.Property(o => o.RejectionReason).HasMaxLength(256);

            entity.HasOne(o => o.Account)
                .WithMany(a => a.Orders)
                .HasForeignKey(o => o.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Security)
                .WithMany()
                .HasForeignKey(o => o.SecurityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(o => new { o.AccountId, o.Status });
        });

        modelBuilder.Entity<Lot>(entity =>
        {
            entity.Property(l => l.UnitCost).HasColumnType("decimal(18,4)");

            entity.HasOne(l => l.Account)
                .WithMany(a => a.Lots)
                .HasForeignKey(l => l.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.Security)
                .WithMany()
                .HasForeignKey(l => l.SecurityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.BuyOrder)
                .WithMany()
                .HasForeignKey(l => l.BuyOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(l => new { l.AccountId, l.SecurityId, l.PurchasedAt });
        });

        modelBuilder.Entity<Trade>(entity =>
        {
            entity.Property(t => t.Price).HasColumnType("decimal(18,4)");
            entity.Property(t => t.RealizedPnL).HasColumnType("decimal(18,4)");

            entity.HasOne(t => t.Order)
                .WithMany()
                .HasForeignKey(t => t.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Account)
                .WithMany(a => a.Trades)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Security)
                .WithMany()
                .HasForeignKey(t => t.SecurityId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
