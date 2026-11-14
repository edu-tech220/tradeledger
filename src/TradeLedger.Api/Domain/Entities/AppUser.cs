using TradeLedger.Api.Domain.Enums;

namespace TradeLedger.Api.Domain.Entities;

public class AppUser
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required byte[] PasswordHash { get; set; }
    public required byte[] PasswordSalt { get; set; }
    public UserRole Role { get; set; } = UserRole.Investor;
    public DateTime CreatedAt { get; set; }

    public Account? Account { get; set; }
}
