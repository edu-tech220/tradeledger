using System.ComponentModel.DataAnnotations;
using TradeLedger.Api.Domain.Enums;

namespace TradeLedger.Api.Contracts;

public record PlaceOrderRequest(
    [Required] string Symbol,
    [Required] OrderSide Side,
    [Required] OrderType Type,
    [Range(1, int.MaxValue)] int Quantity,
    decimal? LimitPrice);

public record OrderResponse(
    int Id,
    string Symbol,
    string Side,
    string Type,
    decimal? LimitPrice,
    int Quantity,
    string Status,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? FilledAt,
    decimal? FilledPrice);
