using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TradeLedger.Web.Services;

namespace TradeLedger.Web.Pages.Trades;

[Authorize]
public class IndexModel(TradeLedgerApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "page")]
    public int PageNumber { get; set; } = 1;

    public PagedResponse<TradeResponse> Trades { get; private set; } = new([], 1, 20, 0, 0);

    public async Task OnGetAsync()
    {
        Trades = await apiClient.GetTradesAsync(PageNumber) ?? Trades;
    }
}
