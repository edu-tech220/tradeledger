using System.Net.Http.Json;

namespace TradeLedger.Web.Services;

public class TradeLedgerApiClient(HttpClient httpClient)
{
    public Task<AuthResponse?> RegisterAsync(string username, string password, CancellationToken ct = default) =>
        PostAsync<AuthResponse>("api/auth/register", new { username, password }, ct);

    public Task<AuthResponse?> LoginAsync(string username, string password, CancellationToken ct = default) =>
        PostAsync<AuthResponse>("api/auth/login", new { username, password }, ct);

    public async Task<IReadOnlyList<SecurityResponse>> GetSecuritiesAsync(CancellationToken ct = default) =>
        await httpClient.GetFromJsonAsync<List<SecurityResponse>>("api/securities", ct) ?? [];

    public Task<SecurityResponse?> UpdateSecurityPriceAsync(string symbol, decimal price, CancellationToken ct = default) =>
        PutAsync<SecurityResponse>($"api/securities/{symbol}/price", new { price }, ct);

    public async Task<AccountSummaryResponse> GetAccountSummaryAsync(CancellationToken ct = default) =>
        await GetAsync<AccountSummaryResponse>("api/accounts/me", ct) ?? throw new InvalidOperationException("Account summary was empty.");

    public async Task<IReadOnlyList<PositionResponse>> GetPositionsAsync(CancellationToken ct = default) =>
        await httpClient.GetFromJsonAsync<List<PositionResponse>>("api/accounts/me/positions", ct) ?? [];

    public Task<PagedResponse<TradeResponse>?> GetTradesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResponse<TradeResponse>>($"api/accounts/me/trades?page={page}&pageSize={pageSize}", ct);

    public Task<OrderResponse?> PlaceOrderAsync(string symbol, string side, string type, int quantity, decimal? limitPrice, CancellationToken ct = default) =>
        PostAsync<OrderResponse>("api/orders", new { symbol, side, type, quantity, limitPrice }, ct);

    public Task<PagedResponse<OrderResponse>?> GetOrdersAsync(string? status, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = $"api/orders?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status))
            query += $"&status={status}";
        return GetAsync<PagedResponse<OrderResponse>>(query, ct);
    }

    public Task<OrderResponse?> CancelOrderAsync(int orderId, CancellationToken ct = default) =>
        PostAsync<OrderResponse>($"api/orders/{orderId}/cancel", new { }, ct);

    public Task<PagedResponse<AdminAccountResponse>?> GetAdminAccountsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResponse<AdminAccountResponse>>($"api/admin/accounts?page={page}&pageSize={pageSize}", ct);

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        var response = await httpClient.GetAsync(url, ct);
        return await ReadOrThrowAsync<T>(response, ct);
    }

    private async Task<T?> PostAsync<T>(string url, object body, CancellationToken ct)
    {
        var response = await httpClient.PostAsJsonAsync(url, body, ct);
        return await ReadOrThrowAsync<T>(response, ct);
    }

    private async Task<T?> PutAsync<T>(string url, object body, CancellationToken ct)
    {
        var response = await httpClient.PutAsJsonAsync(url, body, ct);
        return await ReadOrThrowAsync<T>(response, ct);
    }

    private static async Task<T?> ReadOrThrowAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);

        var problem = await response.Content.ReadFromJsonAsync<ApiProblem>(cancellationToken: ct);
        throw new ApiException(problem?.Detail ?? problem?.Title ?? $"Request failed with status {response.StatusCode}.", problem);
    }
}
