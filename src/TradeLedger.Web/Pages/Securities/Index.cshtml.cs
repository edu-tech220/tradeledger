using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TradeLedger.Web.Services;

namespace TradeLedger.Web.Pages.Securities;

public class IndexModel(TradeLedgerApiClient apiClient) : PageModel
{
    public IReadOnlyList<SecurityResponse> Securities { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Securities = await apiClient.GetSecuritiesAsync();
    }

    // The page only renders the price-update form for Admins, but the real enforcement is
    // the API rejecting the PUT with 403 for any other role - that's the authoritative check.
    public async Task<IActionResult> OnPostAsync(string symbol, decimal newPrice)
    {
        if (!User.IsInRole("Admin"))
            return Forbid();

        try
        {
            await apiClient.UpdateSecurityPriceAsync(symbol, newPrice);
            TempData["Success"] = $"Updated {symbol} to {newPrice:C}.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }
}
