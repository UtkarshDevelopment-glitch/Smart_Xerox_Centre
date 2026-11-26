
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Models;
using Smart_Xerox_Centre.Security;
using System.Security.Cryptography;
using System.Text;

namespace Smart_Xerox_Centre.Controllers
{
    [ApiController]
    [Route("api/qr-flow")]
    public class QrFlowController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly CurrentUserService _currentUser;

        public QrFlowController(
            ApplicationDbContext db,
            CurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        // SHOP OWNER: Generate QR
        [Authorize(Roles = "ShopOwner")]
        [HttpPost("generate/{shopId:long}")]
        public async Task<IActionResult> GenerateQr(long shopId)
        {
            var tenantId = _currentUser.TenantId;

            if (tenantId == null)
                return Unauthorized("Tenant not found.");

            var shop = await _db.Shops.FirstOrDefaultAsync(x =>
                x.ShopId == shopId &&
                x.TenantId == tenantId.Value &&
                x.Status == "Active");

            if (shop == null)
                return NotFound("Active shop not found.");

            var oldQrCodes = await _db.ShopQrcodes
                .Where(x =>
                    x.ShopId == shopId &&
                    x.TenantId == tenantId.Value &&
                    x.IsActive)
                .ToListAsync();

            foreach (var oldQr in oldQrCodes)
            {
                oldQr.IsActive = false;
                oldQr.DeactivatedAtUtc = DateTime.UtcNow;
            }

            var qrValue = Convert.ToHexString(
                RandomNumberGenerator.GetBytes(24));

            var qr = new ShopQrcode
            {
                TenantId = tenantId.Value,
                ShopId = shopId,
                QrcodeValue = qrValue,
                QrimagePath = null,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.ShopQrcodes.Add(qr);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Shop QR generated successfully.",
                qrCodeId = qr.QrcodeId,
                shopId = qr.ShopId,
                shopName = shop.ShopName,
                qrCode = qr.QrcodeValue,
                scanApiUrl = $"{Request.Scheme}://{Request.Host}/api/qr-flow/scan/{qr.QrcodeValue}",
                isActive = qr.IsActive,
                createdAtUtc = qr.CreatedAtUtc
            });
        }

        // CUSTOMER: Scan QR and automatically create Guest Session
        [AllowAnonymous]
        [HttpGet("scan/{qrCode}")]
        public async Task<IActionResult> ScanQr(string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode))
            {
                return BadRequest(new
                {
                    message = "QR Code is required."
                });
            }

            // 1. Validate QR Code
            var qr = await _db.ShopQrcodes
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.QrcodeValue == qrCode &&
                    x.IsActive &&
                    x.DeactivatedAtUtc == null);

            if (qr == null)
            {
                return NotFound(new
                {
                    message = "Invalid or inactive QR Code."
                });
            }

            // 2. Validate Shop
            var shop = await _db.Shops
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ShopId == qr.ShopId &&
                    x.TenantId == qr.TenantId &&
                    x.Status == "Active");

            if (shop == null)
            {
                return NotFound(new
                {
                    message = "Shop is inactive or unavailable."
                });
            }

            // 3. Generate secure Session Token
            string sessionToken = WebEncoders.Base64UrlEncode(
                RandomNumberGenerator.GetBytes(32));

            // 4. Generate SHA256 Hash
            string tokenHash = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(sessionToken)));

            DateTime currentTime = DateTime.UtcNow;

            // 5. Create Guest Customer Session
            var session = new CustomerSession
            {
                SessionId = Guid.NewGuid(),
                TenantId = qr.TenantId,
                ShopId = qr.ShopId,
                CustomerId = null,
                QrcodeId = qr.QrcodeId,
                SessionTokenHash = tokenHash,
                StartedAtUtc = currentTime,
                ExpiresAtUtc = currentTime.AddHours(2),
                EndedAtUtc = null,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString()
            };

            _db.CustomerSessions.Add(session);

            await _db.SaveChangesAsync();

            // 6. Return Shop and Session Details
            return Ok(new
            {
                message = "QR verified and Guest Session created successfully.",

                shop = new
                {
                    shopId = shop.ShopId,
                    shopName = shop.ShopName,
                    address = shop.AddressLine1,
                    city = shop.City,
                    state = shop.State,
                    phone = shop.Phone,
                    email = shop.Email
                },

                session = new
                {
                    sessionId = session.SessionId,
                    tenantId = session.TenantId,
                    shopId = session.ShopId,
                    customerId = session.CustomerId,
                    qrCodeId = session.QrcodeId,
                    sessionToken = sessionToken,
                    startedAtUtc = session.StartedAtUtc,
                    expiresAtUtc = session.ExpiresAtUtc
                }
            });
        }

        // SHOP OWNER: Deactivate QR
        [Authorize(Roles = "ShopOwner")]
        [HttpPut("deactivate/{qrCodeId:long}")]
        public async Task<IActionResult> DeactivateQr(long qrCodeId)
        {
            var tenantId = _currentUser.TenantId;

            if (tenantId == null)
                return Unauthorized();

            var qr = await _db.ShopQrcodes
                .FirstOrDefaultAsync(x =>
                    x.QrcodeId == qrCodeId &&
                    x.TenantId == tenantId.Value);

            if (qr == null)
                return NotFound("QR Code not found.");

            qr.IsActive = false;
            qr.DeactivatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "QR Code deactivated successfully."
            });
        }
    }
}
