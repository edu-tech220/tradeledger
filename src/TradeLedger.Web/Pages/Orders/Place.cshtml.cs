using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TradeLedger.Web.Services;

namespace TradeLedger.Web.Pages.Orders;

[Authorize]
public class PlaceModel(TradeLedgerApiClient apiClient) : PageModel
{
    [BindProperty]
    [Required]
    public string Symbol { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    public string Side { get; set; } = "Buy";

    [BindProperty]
    [Required]
    public string Type { get; set; } = "Market";

    [BindProperty]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [BindProperty]
    public decimal? LimitPrice { get; set; }

    public IEnumerable<SelectListItem> SymbolOptions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var securities = await apiClient.GetSecuritiesAsync();
        SymbolOptions = securities.Select(s => new SelectListItem($"{s.Symbol} - {s.Name} ({s.CurrentPrice:C})", s.Symbol));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Type == "Limit" && LimitPrice is null)
            ModelState.AddModelError(nameof(LimitPrice), "Limit orders require a limit price.");

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        try
        {
            var order = await apiClient.PlaceOrderAsync(Symbol, Side, Type, Quantity, Type == "Limit" ? LimitPrice : null);
            TempData["Success"] = $"Order placed: {order?.Side} {order?.Quantity} {order?.Symbol} - status {order?.Status}.";
            return RedirectToPage("/Orders/Index");
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await OnGetAsync();
            return Page();
        }
    }
}
