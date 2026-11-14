namespace TradeLedger.Api.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public required string Key { get; set; }
    public int AccessTokenMinutes { get; set; } = 60;
}
