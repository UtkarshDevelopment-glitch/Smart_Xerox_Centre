
namespace Smart_Xerox_Centre.DTOs.CustomerSessions;

public sealed class CreateCustomerSessionRequest
{
    public string QRCodeValue { get; set; } = string.Empty;

    public long? CustomerId { get; set; }
}
