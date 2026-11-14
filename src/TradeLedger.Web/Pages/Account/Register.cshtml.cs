using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TradeLedger.Web.Services;

namespace TradeLedger.Web.Pages.Account;

public class RegisterModel(TradeLedgerApiClient apiClient) : PageModel
{
    [BindProperty]
    [Required, StringLength(64, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        AuthResponse? auth;
        try
        {
            auth = await apiClient.RegisterAsync(Username, Password);
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }

        if (auth is null)
        {
            ModelState.AddModelError(string.Empty, "Could not create the account.");
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, auth.Username),
            new(ClaimTypes.Role, auth.Role),
            new("accountId", auth.AccountId.ToString()),
            new(BearerTokenHandler.AccessTokenClaimType, auth.Token),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = auth.ExpiresAt,
        });

        return RedirectToPage("/Dashboard");
    }
}
