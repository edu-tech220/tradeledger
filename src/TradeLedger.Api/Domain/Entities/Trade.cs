using TradeLedger.Api.Domain.Enums;

namespace TradeLedger.Api.Domain.Entities;

public class Trade
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int AccountId { get; set; }
    public int SecurityId { get; set; }
    public OrderSide Side { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal? RealizedPnL { get; set; }
    public DateTime ExecutedAt { get; set; }

    public Order Order { get; set; } = null!;
    public Account Account { get; set; } = null!;
    public Security Security { get; set; } = null!;
}
