namespace Smart_Xerox_Centre.Models.DTOs;

public class CreateUserDto
{
    public long TenantId { get; set; }
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = null!;
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public string Status { get; set; } = "Active";
}