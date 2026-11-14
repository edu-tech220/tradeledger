using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TradeLedger.Web.Services;

namespace TradeLedger.Web.Pages.Orders;

[Authorize]
public class IndexModel(TradeLedgerApiClient apiClient) : PageModel
{
    private static readonly string[] Statuses = ["Pending", "Filled", "Cancelled", "Rejected"];

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true, Name = "page")]
    public int PageNumber { get; set; } = 1;

    public PagedResponse<OrderResponse> Orders { get; private set; } = new([], 1, 20, 0, 0);

    public IEnumerable<SelectListItem> StatusOptions => Statuses.Select(s => new SelectListItem(s, s, s == Status));

    public async Task OnGetAsync()
    {
        Orders = await apiClient.GetOrdersAsync(Status, PageNumber) ?? Orders;
    }

    public async Task<IActionResult> OnPostCancelAsync(int orderId)
    {
        try
        {
            await apiClient.CancelOrderAsync(orderId);
            TempData["Success"] = $"Order {orderId} cancelled.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { status = Status, page = PageNumber });
    }
}
