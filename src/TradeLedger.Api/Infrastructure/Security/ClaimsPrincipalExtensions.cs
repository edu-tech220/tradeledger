using System.Security.Claims;

namespace TradeLedger.Api.Infrastructure.Security;

public static class ClaimsPrincipalExtensions
{
    public static int GetAccountId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtTokenGenerator.AccountIdClaimType)
            ?? throw new InvalidOperationException("The current principal has no accountId claim.");
        return int.Parse(value);
    }
}
