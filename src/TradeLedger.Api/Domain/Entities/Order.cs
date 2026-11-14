using TradeLedger.Api.Domain.Enums;

namespace TradeLedger.Api.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int SecurityId { get; set; }
    public OrderSide Side { get; set; }
    public OrderType Type { get; set; }
    public decimal? LimitPrice { get; set; }
    public int Quantity { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? FilledAt { get; set; }
    public decimal? FilledPrice { get; set; }

    public Account Account { get; set; } = null!;
    public Security Security { get; set; } = null!;
}
