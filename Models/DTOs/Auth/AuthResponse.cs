
namespace Smart_Xerox_Centre.DTOs.Auth;

public class AuthResponse
{
    public long UserId { get; set; }

    public long TenantId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public List<string> Roles { get; set; } = new();

    public string Token { get; set; } = null!;

    public DateTime ExpiresAtUtc { get; set; }
}
