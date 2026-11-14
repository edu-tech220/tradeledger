using System.ComponentModel.DataAnnotations;

namespace TradeLedger.Api.Contracts;

public record RegisterRequest(
    [Required, StringLength(64, MinimumLength = 3)] string Username,
    [Required, StringLength(128, MinimumLength = 8)] string Password);

public record LoginRequest(
    [Required] string Username,
    [Required] string Password);

public record AuthResponse(string Token, DateTime ExpiresAt, string Username, string Role, int AccountId);
