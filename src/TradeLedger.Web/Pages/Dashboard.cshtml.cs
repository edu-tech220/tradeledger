using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TradeLedger.Web.Services;

namespace TradeLedger.Web.Pages;

[Authorize]
public class DashboardModel(TradeLedgerApiClient apiClient) : PageModel
{
    public AccountSummaryResponse Account { get; private set; } = null!;
    public IReadOnlyList<PositionResponse> Positions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Account = await apiClient.GetAccountSummaryAsync();
        Positions = await apiClient.GetPositionsAsync();
    }
}
