using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Models;
using Smart_Xerox_Centre.Security;
using Smart_Xerox_Centre.Services;

namespace Smart_Xerox_Centre.Controllers;

[ApiController]
[Authorize(Roles = "ShopOwner,ShopStaff")]
[Route("api/print-tokens")]
public sealed class PrintTokenCollectionController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly CurrentUserService _current;
    private readonly ITokenHasher _hasher;

    public PrintTokenCollectionController(ApplicationDbContext db, CurrentUserService current, ITokenHasher hasher)
    {
        _db = db;
        _current = current;
        _hasher = hasher;
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyPrintTokenRequest request)
    {
        if (!_current.TenantId.HasValue || !_current.UserId.HasValue) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Token)) return BadRequest(new { message = "Token is required." });

        var hash = _hasher.Hash(request.Token.Trim());
        var token = await _db.PrintTokens.AsNoTracking().FirstOrDefaultAsync(x =>
            x.TenantId == _current.TenantId.Value && x.TokenHash == hash && !x.IsRevoked);

        if (token == null) return NotFound(new { valid = false, message = "Invalid token." });
        if (token.UsedAtUtc.HasValue) return Conflict(new { valid = false, message = "Token already used." });
        if (token.ExpiresAtUtc.HasValue && token.ExpiresAtUtc.Value <= DateTime.UtcNow)
            return Conflict(new { valid = false, message = "Token expired." });

        return Ok(new { valid = true, token.PrintTokenId, token.OrderId, token.ShopId, token.TokenDisplay });
    }

    [HttpPost("collect")]
    public async Task<IActionResult> Collect([FromBody] CollectPrintTokenRequest request)
    {
        if (!_current.TenantId.HasValue || !_current.UserId.HasValue) return Unauthorized();
        var hash = _hasher.Hash(request.Token?.Trim() ?? "");
        var token = await _db.PrintTokens.FirstOrDefaultAsync(x =>
            x.TenantId == _current.TenantId.Value && x.TokenHash == hash && !x.IsRevoked);

        if (token == null) return NotFound(new { message = "Invalid token." });
        if (token.UsedAtUtc.HasValue) return Conflict(new { message = "Token already collected." });
        if (token.ExpiresAtUtc.HasValue && token.ExpiresAtUtc.Value <= DateTime.UtcNow)
            return Conflict(new { message = "Token expired." });

        var now = DateTime.UtcNow;
        token.UsedAtUtc = now;
        token.UsedByUserId = _current.UserId.Value;

        _db.CollectionLogs.Add(new CollectionLog
        {
            TenantId = token.TenantId,
            ShopId = token.ShopId,
            OrderId = token.OrderId,
            PrintTokenId = token.PrintTokenId,
            StaffUserId = _current.UserId.Value,
            CollectionStatus = "Collected",
            Remarks = request.Remarks,
            CollectedAtUtc = now
        });

        await _db.SaveChangesAsync();
        return Ok(new { message = "Order collection recorded.", token.OrderId, collectedAtUtc = now });
    }
}

public sealed class VerifyPrintTokenRequest { public string Token { get; set; } = string.Empty; }
public sealed class CollectPrintTokenRequest { public string Token { get; set; } = string.Empty; public string? Remarks { get; set; } }
