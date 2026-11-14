using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeLedger.Api.Contracts;
using TradeLedger.Api.Domain.Entities;
using TradeLedger.Api.Domain.Enums;
using TradeLedger.Api.Infrastructure.Security;
using TradeLedger.Api.Services;

namespace TradeLedger.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> PlaceOrder(PlaceOrderRequest request, CancellationToken ct)
    {
        var accountId = User.GetAccountId();
        var order = await orderService.PlaceOrderAsync(
            accountId, request.Symbol.ToUpperInvariant(), request.Side, request.Type, request.Quantity, request.LimitPrice, ct);

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, ToResponse(order));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(int id, CancellationToken ct)
    {
        var accountId = User.GetAccountId();
        var order = await orderService.GetOrderAsync(accountId, id, ct);
        return Ok(ToResponse(order));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<OrderResponse>>> GetOrders(
        [FromQuery] OrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var accountId = User.GetAccountId();
        var result = await orderService.GetOrdersAsync(accountId, status, page, pageSize, ct);
        var items = result.Items.Select(ToResponse).ToList();
        return Ok(new PagedResponse<OrderResponse>(items, result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<OrderResponse>> Cancel(int id, CancellationToken ct)
    {
        var accountId = User.GetAccountId();
        var order = await orderService.CancelOrderAsync(accountId, id, ct);
        return Ok(ToResponse(order));
    }

    private static OrderResponse ToResponse(Order order) => new(
        order.Id,
        order.Security.Symbol,
        order.Side.ToString(),
        order.Type.ToString(),
        order.LimitPrice,
        order.Quantity,
        order.Status.ToString(),
        order.RejectionReason,
        order.CreatedAt,
        order.FilledAt,
        order.FilledPrice);
}
