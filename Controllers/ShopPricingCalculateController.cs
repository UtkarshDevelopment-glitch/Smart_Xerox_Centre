using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.DTOs.Pricing;

namespace Smart_Xerox_Centre.Controllers;

[ApiController]
[Route("api/shops/{shopId:long}/pricing")]
public sealed class ShopPricingCalculateController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ShopPricingCalculateController(ApplicationDbContext db) => _db = db;

    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate(long shopId, [FromBody] CalculatePriceRequest request)
    {
        if (request.ShopId != 0 && request.ShopId != shopId)
            return BadRequest(new { message = "ShopId in route and body do not match." });
        if (request.PagesToPrint <= 0 || request.Copies <= 0)
            return BadRequest(new { message = "PagesToPrint and Copies must be greater than zero." });

        var shop = await _db.Shops.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ShopId == shopId && x.Status == "Active");
        if (shop == null) return NotFound(new { message = "Active shop not found." });

        var now = DateTime.UtcNow;
        var pricing = await _db.ShopPricings.AsNoTracking()
            .Where(x => x.TenantId == shop.TenantId && x.ShopId == shopId && x.IsActive
                && x.ColorMode == request.ColorMode && x.Sides == request.Sides
                && x.PaperSize == request.PaperSize
                && x.EffectiveFromUtc <= now
                && (x.EffectiveToUtc == null || x.EffectiveToUtc > now))
            .OrderByDescending(x => x.EffectiveFromUtc)
            .FirstOrDefaultAsync();

        if (pricing == null)
            return NotFound(new { message = "No active pricing configuration matches these print settings." });

        var billablePages = checked((long)request.PagesToPrint * request.Copies);
        var subTotal = billablePages * pricing.RatePerPage + pricing.AdditionalCharge;
        var tax = subTotal * pricing.TaxRate / 100m;
        var total = decimal.Round(subTotal + tax, 2, MidpointRounding.AwayFromZero);

        return Ok(new
        {
            shopId,
            pricing.ShopPricingId,
            request.ColorMode,
            request.Sides,
            request.PaperSize,
            request.PagesToPrint,
            request.Copies,
            billablePages,
            pricing.RatePerPage,
            pricing.AdditionalCharge,
            subTotal,
            taxRate = pricing.TaxRate,
            taxAmount = tax,
            totalAmount = total,
            currencyCode = "INR"
        });
    }
}
