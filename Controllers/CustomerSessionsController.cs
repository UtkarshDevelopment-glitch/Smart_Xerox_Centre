
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.DTOs.CustomerSessions;
using Smart_Xerox_Centre.Models;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Xerox_Centre.Controllers;

[ApiController]
[Route("api/customer-sessions")]
//[ApiExplorerSettings(IgnoreApi = true)]

public sealed class CustomerSessionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CustomerSessionsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateSession(
    [FromBody] Smart_Xerox_Centre.DTOs.CustomerSessions.CreateCustomerSessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.QRCodeValue))
        {
            return BadRequest(new
            {
                message = "QRCodeValue is required."
            });
        }

        // 1. Validate QR Code
        var qrCode = await _db.ShopQrcodes
            .Include(x => x.Shop)
            .FirstOrDefaultAsync(x =>
                x.QrcodeValue == request.QRCodeValue &&
                x.IsActive &&
                x.DeactivatedAtUtc == null);

        if (qrCode == null)
        {
            return NotFound(new
            {
                message = "Invalid or inactive QR Code."
            });
        }

        // 2. Validate Shop
        if (qrCode.Shop == null ||
            !string.Equals(
                qrCode.Shop.Status,
                "Active",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Shop is inactive."
            });
        }

        // 3. Validate Customer if provided
        if (request.CustomerId.HasValue)
        {
            bool customerExists = await _db.Customers.AnyAsync(x =>
                x.CustomerId == request.CustomerId.Value &&
                x.TenantId == qrCode.TenantId);

            if (!customerExists)
            {
                return BadRequest(new
                {
                    message = "Invalid CustomerId."
                });
            }
        }

        // 4. Generate secure session token
        string sessionToken = WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(32));

        // 5. Store only token hash
        string tokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(sessionToken)));

        DateTime currentTime = DateTime.UtcNow;

        // 6. Create Customer Session
        var session = new CustomerSession
        {
            SessionId = Guid.NewGuid(),
            TenantId = qrCode.TenantId,
            ShopId = qrCode.ShopId,
            CustomerId = request.CustomerId,
            QrcodeId = qrCode.QrcodeId,
            SessionTokenHash = tokenHash,
            StartedAtUtc = currentTime,
            ExpiresAtUtc = currentTime.AddHours(2),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        _db.CustomerSessions.Add(session);

        await _db.SaveChangesAsync();

        // 7. Return session details
        return Ok(new
        {
            message = "Customer session created successfully.",
            sessionId = session.SessionId,
            tenantId = session.TenantId,
            shopId = session.ShopId,
            customerId = session.CustomerId,
            qrcodeId = session.QrcodeId,
            sessionToken = sessionToken,
            startedAtUtc = session.StartedAtUtc,
            expiresAtUtc = session.ExpiresAtUtc
        });
    }
}
