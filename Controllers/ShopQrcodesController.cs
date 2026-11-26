
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using QRCoder;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Models;
using Smart_Xerox_Centre.Security;
using Smart_Xerox_Centre.DTOs.ShopQrcodes;

namespace Smart_Xerox_Centre.Controllers;

[Authorize(Roles = "ShopOwner")]
[ApiController]
[Route("api/shop-qrcodes")]
public class ShopQrcodesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly CurrentUserService _currentUser;

    public ShopQrcodesController(
        ApplicationDbContext db,
        CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateQrCode(
        [FromBody] GenerateShopQrCodeRequest request)
    {
        long? tenantId = _currentUser.TenantId;

        if (!tenantId.HasValue)
            return Unauthorized();

        if (request.ShopId <= 0)
            return BadRequest(new { message = "Valid ShopId is required." });

        var shop = await _db.Shops.FirstOrDefaultAsync(x =>
            x.ShopId == request.ShopId &&
            x.TenantId == tenantId.Value &&
            x.Status == "Active");

        if (shop == null)
            return NotFound(new { message = "Active shop not found." });

        // Check existing active QR
        var existingQr = await _db.ShopQrcodes.FirstOrDefaultAsync(x =>
            x.ShopId == shop.ShopId &&
            x.TenantId == tenantId.Value &&
            x.IsActive &&
            x.DeactivatedAtUtc == null);

        if (existingQr != null)
        {
            return Ok(new
            {
                message = "Active QR Code already exists.",
                qrCodeId = existingQr.QrcodeId,
                qrCodeValue = existingQr.QrcodeValue
            });
        }

        string qrValue = $"SMARTXEROX-{Guid.NewGuid():N}";

        var qrCode = new ShopQrcode
        {
            TenantId = tenantId.Value,
            ShopId = shop.ShopId,
            QrcodeValue = qrValue,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.ShopQrcodes.Add(qrCode);
        await _db.SaveChangesAsync();

        using QRCodeGenerator generator = new QRCodeGenerator();
        using QRCodeData data = generator.CreateQrCode(
            qrValue,
            QRCodeGenerator.ECCLevel.Q);

        var pngQrCode = new PngByteQRCode(data);
        byte[] qrImageBytes = pngQrCode.GetGraphic(20);

        string base64Image = Convert.ToBase64String(qrImageBytes);

        return Ok(new
        {
            message = "QR Code generated successfully.",
            qrCodeId = qrCode.QrcodeId,
            shopId = qrCode.ShopId,
            qrCodeValue = qrCode.QrcodeValue,
            qrImageBase64 = base64Image
        });
    }

    [HttpGet("shop/{shopId:long}")]
    public async Task<IActionResult> GetShopQrCode(long shopId)
    {
        long? tenantId = _currentUser.TenantId;

        if (!tenantId.HasValue)
            return Unauthorized();

        var qr = await _db.ShopQrcodes
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ShopId == shopId &&
                x.TenantId == tenantId.Value &&
                x.IsActive &&
                x.DeactivatedAtUtc == null);

        if (qr == null)
            return NotFound(new { message = "QR Code not found." });

        return Ok(new
        {
            qr.QrcodeId,
            qr.ShopId,
            qr.QrcodeValue,
            qr.IsActive,
            qr.CreatedAtUtc
        });
    }
}
