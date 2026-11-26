using Microsoft.AspNetCore.Mvc;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Models;

namespace Smart_Xerox_Centre.Controllers;

[Route("api/[controller]")]
public class ShopPricingsController : CrudController<ShopPricing, long>
{
    public ShopPricingsController(ApplicationDbContext db) : base(db) { }
    protected override string KeyPropertyName => nameof(ShopPricing.ShopPricingId);
}
