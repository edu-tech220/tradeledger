using System.ComponentModel.DataAnnotations;

namespace TradeLedger.Api.Contracts;

public record SecurityResponse(string Symbol, string Name, decimal CurrentPrice, DateTime UpdatedAt);

public record UpdatePriceRequest([Range(0.0001, double.MaxValue)] decimal Price);
