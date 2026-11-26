
namespace Smart_Xerox_Centre.DTOs.Auth;

public class RegisterRequest
{
    public long TenantId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string Password { get; set; } = null!;
}
