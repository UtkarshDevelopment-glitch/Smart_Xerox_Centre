
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Models;
using System.Security.Cryptography;
using System.Text;

namespace Smart_Xerox_Centre.Controllers
{
    [ApiController]
    [Route("api/customer-session-flow")]
    public class CustomerSessionFlowController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CustomerSessionFlowController(ApplicationDbContext db)
        {
            _db = db;
        }

        // CUSTOMER: Create session after QR scan
        [AllowAnonymous]
        [HttpPost("create")]
        public async Task<IActionResult> CreateSession(
            [FromBody] CreateCustomerSessionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.QrCode))
                return BadRequest("QR Code is required.");

            var qr = await _db.ShopQrcodes
                .FirstOrDefaultAsync(x =>
                    x.QrcodeValue == request.QrCode &&
                    x.IsActive);

            if (qr == null)
                return NotFound("Invalid or inactive QR Code.");

            var shop = await _db.Shops
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ShopId == qr.ShopId &&
                    x.TenantId == qr.TenantId &&
                    x.Status == "Active");

            if (shop == null)
                return NotFound("Shop is inactive or unavailable.");

            // Generate secure session token
            var sessionToken = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32));

            // Store only SHA256 hash
            var tokenHash = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(sessionToken)));

            var now = DateTime.UtcNow;

            var session = new CustomerSession
            {
                SessionId = Guid.NewGuid(),
                TenantId = qr.TenantId,
                ShopId = qr.ShopId,
                CustomerId = null,
                QrcodeId = qr.QrcodeId,
                SessionTokenHash = tokenHash,
                StartedAtUtc = now,
                ExpiresAtUtc = now.AddMinutes(30),
                EndedAtUtc = null,
                IpAddress = HttpContext.Connection
                    .RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            };

            _db.CustomerSessions.Add(session);

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Customer session created successfully.",
                sessionId = session.SessionId,
                sessionToken = sessionToken,
                tenantId = session.TenantId,
                shopId = session.ShopId,
                shopName = shop.ShopName,
                startedAtUtc = session.StartedAtUtc,
                expiresAtUtc = session.ExpiresAtUtc
            });
        }

        // CUSTOMER: Validate session token

        [AllowAnonymous]
        [HttpGet("validate")]
        public async Task<IActionResult> ValidateSession(
            [FromHeader(Name = "X-Session-Token")] string sessionToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                return Unauthorized("Session token is required.");

            var tokenHash = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(sessionToken)));

            var now = DateTime.UtcNow;

            var session = await _db.CustomerSessions
                .AsNoTracking()
                .Include(x => x.Shop)
                .FirstOrDefaultAsync(x =>
                    x.SessionTokenHash == tokenHash &&
                    x.EndedAtUtc == null &&
                    x.ExpiresAtUtc > now);

            if (session == null)
                return Unauthorized("Session is invalid or expired.");

            return Ok(new
            {
                message = "Session is valid.",
                sessionId = session.SessionId,
                tenantId = session.TenantId,
                shopId = session.ShopId,
                shopName = session.Shop.ShopName,
                expiresAtUtc = session.ExpiresAtUtc,
                isValid = true
            });
        }


        // CUSTOMER: End session

        [AllowAnonymous]
        [HttpPost("end")]
        public async Task<IActionResult> EndSession(
            [FromHeader(Name = "X-Session-Token")] string sessionToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                return Unauthorized("Session token is required.");

            var tokenHash = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(sessionToken)));

            var session = await _db.CustomerSessions
                .FirstOrDefaultAsync(x =>
                    x.SessionTokenHash == tokenHash &&
                    x.EndedAtUtc == null &&
                    x.ExpiresAtUtc > DateTime.UtcNow);

            if (session == null)
                return NotFound("Active session not found.");

            session.EndedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Customer session ended successfully.",
                sessionId = session.SessionId,
                endedAtUtc = session.EndedAtUtc
            });
        }

    }

    public class CreateCustomerSessionRequest
    {
        public string QrCode { get; set; } = string.Empty;
    }
}
