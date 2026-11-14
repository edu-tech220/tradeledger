namespace TradeLedger.Api.Domain.Entities;

public class Lot
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int SecurityId { get; set; }
    public int BuyOrderId { get; set; }
    public int OriginalQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime PurchasedAt { get; set; }

    // Application-managed concurrency token, see Account.Version.
    public uint Version { get; set; }

    public Account Account { get; set; } = null!;
    public Security Security { get; set; } = null!;
    public Order BuyOrder { get; set; } = null!;
}
