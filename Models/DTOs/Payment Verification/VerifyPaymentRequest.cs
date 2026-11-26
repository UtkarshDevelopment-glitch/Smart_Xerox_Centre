
namespace Smart_Xerox_Centre.DTOs.Payments
{
    public class VerifyPaymentRequest
    {
        public long PaymentId { get; set; }

        public string RazorpayOrderId { get; set; } = string.Empty;

        public string RazorpayPaymentId { get; set; } = string.Empty;

        public string RazorpaySignature { get; set; } = string.Empty;
    }
}
