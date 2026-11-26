using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;

namespace Smart_Xerox_Centre.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public DashboardController(ApplicationDbContext db) => _db = db;

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(long? tenantId, long? shopId)
    {
        var orders = _db.Orders.AsNoTracking().AsQueryable();
        var jobs = _db.PrintJobs.AsNoTracking().AsQueryable();
        if (tenantId.HasValue) { orders = orders.Where(x => x.TenantId == tenantId); jobs = jobs.Where(x => x.TenantId == tenantId); }
        if (shopId.HasValue) { orders = orders.Where(x => x.ShopId == shopId); jobs = jobs.Where(x => x.ShopId == shopId); }

        return Ok(new {
            TotalOrders = await orders.CountAsync(),
            PendingOrders = await orders.CountAsync(x => x.OrderStatus == "Pending"),
            TotalPrintJobs = await jobs.CountAsync(),
            PendingPrintJobs = await jobs.CountAsync(x => x.JobStatus == "Queued" || x.JobStatus == "Pending"),
            CompletedPrintJobs = await jobs.CountAsync(x => x.JobStatus == "Completed")
        });
    }
}
