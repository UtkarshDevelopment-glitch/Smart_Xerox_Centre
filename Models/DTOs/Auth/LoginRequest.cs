
namespace Smart_Xerox_Centre.DTOs.Auth;

public class LoginRequest
{
    public long TenantId { get; set; }

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;
}
