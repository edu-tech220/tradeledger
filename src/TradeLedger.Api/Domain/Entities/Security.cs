namespace TradeLedger.Api.Domain.Entities;

public class Security
{
    public int Id { get; set; }
    public required string Symbol { get; set; }
    public required string Name { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime UpdatedAt { get; set; }
}
