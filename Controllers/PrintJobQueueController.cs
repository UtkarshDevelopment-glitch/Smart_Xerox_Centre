using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Models;

namespace Smart_Xerox_Centre.Controllers;

[ApiController]
[Authorize(Roles = "ShopOwner,ShopStaff,SuperAdmin")]
[Route("api/print-jobs")]
public sealed class PrintJobQueueController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PrintJobQueueController(ApplicationDbContext db) => _db = db;

    [HttpGet("shop/{shopId:long}/queue")]
    public async Task<IActionResult> GetQueue(long shopId)
    {
        var shop = await _db.Shops.AsNoTracking().FirstOrDefaultAsync(x => x.ShopId == shopId);
        if (shop == null) return NotFound();

        var jobs = await _db.PrintJobs.AsNoTracking()
            .Where(x => x.TenantId == shop.TenantId && x.ShopId == shopId
                && (x.JobStatus == "Queued" || x.JobStatus == "Retry"))
            .OrderByDescending(x => x.Priority).ThenBy(x => x.QueuedAtUtc)
            .Select(x => new { x.PrintJobId, x.JobGuid, x.OrderId, x.PrinterId, x.JobStatus, x.Priority, x.QueuedAtUtc })
            .ToListAsync();

        return Ok(jobs);
    }

    // This endpoint records a status transition. Agent authentication must be enforced
    // at gateway/reverse-proxy level until a dedicated agent-key middleware is installed.
    [HttpPost("{printJobId:long}/status")]
    public async Task<IActionResult> UpdateStatus(long printJobId, [FromBody] UpdatePrintJobStatusRequest request)
    {
        var job = await _db.PrintJobs.FirstOrDefaultAsync(x => x.PrintJobId == printJobId);
        if (job == null) return NotFound();

        var allowed = new[] { "Queued", "Printing", "Completed", "Failed", "Retry" };
        if (!allowed.Contains(request.NewStatus))
            return BadRequest(new { message = "Unsupported job status." });

        var old = job.JobStatus;
        job.JobStatus = request.NewStatus;
        if (request.NewStatus == "Printing") job.StartedAtUtc ??= DateTime.UtcNow;
        if (request.NewStatus == "Completed") job.CompletedAtUtc = DateTime.UtcNow;
        job.LastError = request.ErrorMessage;

        _db.PrintJobStatuses.Add(new PrintJobStatus
        {
            PrintJobId = job.PrintJobId,
            OldStatus = old,
            NewStatus = request.NewStatus,
            ChangedByType = "PrintAgent",
            ChangedAtUtc = DateTime.UtcNow,
            Remarks = request.ErrorMessage
        });
        _db.PrintLogs.Add(new PrintLog
        {
            PrintJobId = job.PrintJobId,
            LogLevel = request.NewStatus == "Failed" ? "Error" : "Information",
            Message = request.ErrorMessage ?? $"Print job status changed to {request.NewStatus}.",
            LoggedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Ok(new { job.PrintJobId, job.JobStatus, job.StartedAtUtc, job.CompletedAtUtc });
    }
}

public sealed class UpdatePrintJobStatusRequest
{
    public string NewStatus { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
