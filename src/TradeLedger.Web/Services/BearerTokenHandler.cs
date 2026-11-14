using System.Net.Http.Headers;

namespace TradeLedger.Web.Services;

/// <summary>Attaches the JWT stashed in the signed-in user's cookie as a Bearer header on every outgoing API call.</summary>
public class BearerTokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    public const string AccessTokenClaimType = "access_token";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = httpContextAccessor.HttpContext?.User.FindFirst(AccessTokenClaimType)?.Value;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return base.SendAsync(request, cancellationToken);
    }
}
