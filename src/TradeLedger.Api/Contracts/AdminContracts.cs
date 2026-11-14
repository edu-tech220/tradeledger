namespace TradeLedger.Api.Contracts;

public record AdminAccountResponse(int AccountId, string Username, string Role, decimal CashBalance);
