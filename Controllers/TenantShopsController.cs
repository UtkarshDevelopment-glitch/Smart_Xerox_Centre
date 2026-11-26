
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Security;

namespace Smart_Xerox_Centre.Controllers;

[ApiController]
[Route("api/tenant-shops")]
[Authorize(Roles = "ShopOwner")]
public class TenantShopsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly CurrentUserService _currentUser;

    public TenantShopsController(
        ApplicationDbContext db,
        CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // GET: api/tenant-shops
    [HttpGet]
    public async Task<IActionResult> GetShops()
    {
        if (!_currentUser.TenantId.HasValue)
            return Unauthorized("Tenant information not found.");

        var tenantId = _currentUser.TenantId.Value;

        var shops = await _db.Shops
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => new
            {
                x.ShopId,
                x.TenantId,
                x.ShopCode,
                x.ShopName,
                x.AddressLine1,
                x.AddressLine2,
                x.City,
                x.State,
                x.PostalCode,
                x.Country,
                x.ContactPerson,
                x.Email,
                x.Phone,
                x.TimeZone,
                x.Status,
                x.CreatedAtUtc,
                x.UpdatedAtUtc
            })
            .ToListAsync();

        return Ok(shops);
    }

    // GET: api/tenant-shops/1
    [HttpGet("{shopId:long}")]
    public async Task<IActionResult> GetShopById(long shopId)
    {
        if (!_currentUser.TenantId.HasValue)
            return Unauthorized("Tenant information not found.");

        var tenantId = _currentUser.TenantId.Value;

        var shop = await _db.Shops
            .AsNoTracking()
            .Where(x =>
                x.ShopId == shopId &&
                x.TenantId == tenantId)
            .Select(x => new
            {
                x.ShopId,
                x.TenantId,
                x.ShopCode,
                x.ShopName,
                x.AddressLine1,
                x.AddressLine2,
                x.City,
                x.State,
                x.PostalCode,
                x.Country,
                x.ContactPerson,
                x.Email,
                x.Phone,
                x.TimeZone,
                x.Status,
                x.CreatedAtUtc,
                x.UpdatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (shop == null)
            return NotFound("Shop not found in your Tenant.");

        return Ok(shop);
    }
}
