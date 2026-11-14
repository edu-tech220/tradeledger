using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TradeLedger.Web.Services;

namespace TradeLedger.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AccountsModel(TradeLedgerApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "page")]
    public int PageNumber { get; set; } = 1;

    public PagedResponse<AdminAccountResponse> Accounts { get; private set; } = new([], 1, 20, 0, 0);

    public async Task OnGetAsync()
    {
        Accounts = await apiClient.GetAdminAccountsAsync(PageNumber) ?? Accounts;
    }
}
