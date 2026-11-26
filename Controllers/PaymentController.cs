
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.DTOs.Payments;
using Smart_Xerox_Centre.Models;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Smart_Xerox_Centre.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class PaymentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public PaymentController(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment(
    [FromBody] CreatePaymentRequest request,
    [FromHeader(Name = "X-Session-Token")] string? sessionToken)
        {
            try
            {
                // 1. Validate session token
                //string? sessionToken = Request.Headers["X-Session-Token"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(sessionToken))
                    return Unauthorized(new { message = "Session token is required." });

                string tokenHash = Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(sessionToken)));

                var session = await _context.CustomerSessions
                    .FirstOrDefaultAsync(x =>
                        x.SessionTokenHash == tokenHash &&
                        x.EndedAtUtc == null &&
                        x.ExpiresAtUtc > DateTime.UtcNow);

                if (session == null)
                    return Unauthorized(new { message = "Invalid or expired session." });

                // 2. Get order for this session
                var order = await _context.Orders
                    .FirstOrDefaultAsync(x =>
                        x.OrderId == request.OrderId &&
                        x.TenantId == session.TenantId &&
                        x.ShopId == session.ShopId &&
                        x.SessionId == session.SessionId);

                if (order == null)
                    return NotFound(new { message = "Order not found." });

                if (order.OrderStatus != "Created")
                    return BadRequest(new { message = "Order is not available for payment." });

                if (order.TotalAmount <= 0)
                    return BadRequest(new { message = "Invalid order amount." });

                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(x =>
                        x.OrderId == order.OrderId &&
                        (x.PaymentStatus == "Created" ||
                         x.PaymentStatus == "Pending"));

                if (existingPayment != null &&
                    !string.IsNullOrWhiteSpace(existingPayment.GatewayOrderId))
                {
                    string? existingKeyId = _configuration["Razorpay:KeyId"];

                    return Ok(new
                    {
                        message = "Existing Razorpay order found.",
                        paymentId = existingPayment.PaymentId,
                        orderId = order.OrderId,
                        orderNumber = order.OrderNumber,
                        razorpayOrderId = existingPayment.GatewayOrderId,
                        keyId = existingKeyId,
                        amount = Convert.ToInt64(existingPayment.Amount * 100),
                        currency = existingPayment.CurrencyCode,
                        name = "Smart Xerox Centre",
                        description = "Xerox Print Order Payment"
                    });
                }

                // 4. Read Razorpay credentials
                string? keyId = _configuration["Razorpay:KeyId"];
                string? keySecret = _configuration["Razorpay:KeySecret"];

                if (string.IsNullOrWhiteSpace(keyId) ||
                    string.IsNullOrWhiteSpace(keySecret))
                {
                    return StatusCode(500, new
                    {
                        message = "Razorpay credentials are not configured."
                    });
                }

                // 5. Convert amount into paise
                long amountInPaise = Convert.ToInt64(
                    Math.Round(order.TotalAmount * 100,
                    MidpointRounding.AwayFromZero));

                // 6. Create Razorpay Order
                var client = _httpClientFactory.CreateClient();

                string credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", credentials);

                var razorpayRequest = new
                {
                    amount = amountInPaise,
                    currency = "INR",
                    receipt = order.OrderNumber
                };

                string json = JsonSerializer.Serialize(razorpayRequest);

                var content = new StringContent(
                    json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(
                    "https://api.razorpay.com/v1/orders", content);

                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        message = "Razorpay order creation failed.",
                        details = responseBody
                    });
                }

                using JsonDocument document = JsonDocument.Parse(responseBody);

                string razorpayOrderId =
                    document.RootElement.GetProperty("id").GetString()!;

                // 7. Save payment in database
                var payment = new Payment
                {
                    TenantId = order.TenantId,
                    ShopId = order.ShopId,
                    OrderId = order.OrderId,
                    GatewayName = "Razorpay",
                    GatewayOrderId = razorpayOrderId,
                    Amount = order.TotalAmount,
                    CurrencyCode = "INR",
                    PaymentStatus = "Created",
                    GatewaySignatureVerified = false,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // 8. Return details to frontend
                return Ok(new
                {
                    message = "Razorpay order created successfully.",
                    paymentId = payment.PaymentId,
                    orderId = order.OrderId,
                    orderNumber = order.OrderNumber,
                    razorpayOrderId = razorpayOrderId,
                    keyId = keyId,
                    amount = amountInPaise,
                    currency = "INR",
                    name = "Smart Xerox Centre",
                    description = "Xerox Print Order Payment"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Payment creation failed.",
                    error = ex.Message
                });
            }
        }

    
[HttpPost("verify")]
        public async Task<IActionResult> VerifyPayment(
    [FromBody] VerifyPaymentRequest request,
    [FromHeader(Name = "X-Session-Token")] string? sessionToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionToken))
                    return Unauthorized(new { message = "Session token required." });

                string tokenHash = Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(sessionToken)));

                var session = await _context.CustomerSessions
                    .FirstOrDefaultAsync(x =>
                        x.SessionTokenHash == tokenHash &&
                        x.EndedAtUtc == null &&
                        x.ExpiresAtUtc > DateTime.UtcNow);

                if (session == null)
                    return Unauthorized(new { message = "Invalid or expired session." });

                var payment = await _context.Payments
                    .Include(x => x.Order)
                    .FirstOrDefaultAsync(x =>
                        x.PaymentId == request.PaymentId &&
                        x.TenantId == session.TenantId &&
                        x.ShopId == session.ShopId &&
                        x.Order.SessionId == session.SessionId);

                if (payment == null)
                    return NotFound(new { message = "Payment not found." });

                if (string.IsNullOrWhiteSpace(payment.GatewayOrderId) ||
                    string.IsNullOrWhiteSpace(request.RazorpayOrderId) ||
                    string.IsNullOrWhiteSpace(request.RazorpayPaymentId) ||
                    string.IsNullOrWhiteSpace(request.RazorpaySignature))
                {
                    return BadRequest(new { message = "Payment verification details are required." });
                }

                if (payment.GatewayOrderId != request.RazorpayOrderId)
                    return BadRequest(new { message = "Razorpay Order ID mismatch." });

                string? secret = _configuration["Razorpay:KeySecret"];

                if (string.IsNullOrWhiteSpace(secret))
                    return StatusCode(500, new { message = "Razorpay secret not configured." });

                string payload = payment.GatewayOrderId + "|" + request.RazorpayPaymentId;

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));

                byte[] generatedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));

                string generatedSignature = Convert.ToHexString(generatedHash)
                    .ToLowerInvariant();

                byte[] receivedSignature;

                try
                {
                    receivedSignature = Convert.FromHexString(request.RazorpaySignature);
                }
                catch
                {
                    return BadRequest(new { message = "Invalid Razorpay signature format." });
                }

                if (!CryptographicOperations.FixedTimeEquals(
                    generatedHash, receivedSignature))
                {
                    return BadRequest(new { message = "Invalid payment signature." });
                }

                // Verify payment status directly from Razorpay
                var client = _httpClientFactory.CreateClient();

                string keyId = _configuration["Razorpay:KeyId"]!;

                string credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{keyId}:{secret}"));

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", credentials);

                var response = await client.GetAsync(
                    $"https://api.razorpay.com/v1/payments/{request.RazorpayPaymentId}");

                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, new
                    {
                        message = "Unable to verify payment status with Razorpay.",
                        details = responseBody
                    });

                using JsonDocument document = JsonDocument.Parse(responseBody);

                var root = document.RootElement;

                string razorpayStatus = root.GetProperty("status").GetString() ?? "";

                string gatewayOrderId =
                    root.GetProperty("order_id").GetString() ?? "";

                long razorpayAmount = root.GetProperty("amount").GetInt64();

                string currency = root.GetProperty("currency").GetString() ?? "";

                if (gatewayOrderId != payment.GatewayOrderId ||
                    razorpayAmount != Convert.ToInt64(payment.Amount * 100) ||
                    currency != payment.CurrencyCode)
                {
                    return BadRequest(new { message = "Payment details do not match our order." });
                }

                payment.GatewayPaymentId = request.RazorpayPaymentId;
                payment.GatewaySignatureVerified = true;

                string? method = root.TryGetProperty("method", out var methodElement)
                    ? methodElement.GetString()
                    : null;

                payment.PaymentMethod = method;

                if (razorpayStatus == "captured")
                {
                    payment.PaymentStatus = "Successful";
                    payment.PaidAtUtc = DateTime.UtcNow;
                    payment.Order.OrderStatus = "Paid";
                }
                else if (razorpayStatus == "authorized")
                {
                    payment.PaymentStatus = "Pending";
                }
                else
                {
                    payment.PaymentStatus = "Failed";
                }

                var transaction = new PaymentTransaction
                {
                    PaymentId = payment.PaymentId,
                    EventType = "CheckoutPaymentVerified",
                    GatewayStatus = razorpayStatus,
                    SignatureValid = true,
                    PayloadJson = responseBody,
                    ProcessedAtUtc = DateTime.UtcNow,
                    ReceivedAtUtc = DateTime.UtcNow
                };

                _context.PaymentTransactions.Add(transaction);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = razorpayStatus == "captured"
                        ? "Payment verified and captured successfully."
                        : $"Payment verified. Current Razorpay status: {razorpayStatus}",
                    paymentId = payment.PaymentId,
                    razorpayPaymentId = payment.GatewayPaymentId,
                    razorpayOrderId = payment.GatewayOrderId,
                    paymentStatus = payment.PaymentStatus,
                    signatureVerified = payment.GatewaySignatureVerified,
                    orderStatus = payment.Order.OrderStatus
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Payment verification failed.",
                    error = ex.Message
                });
            }
        }

    }
}
