
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Models;
using Smart_Xerox_Centre.DTOs.PrintJobs;
using System.Security.Cryptography;
using System.Text;

namespace Smart_Xerox_Centre.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrintJobsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PrintJobsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/PrintJobs/generate
        [HttpPost("generate")]
        public async Task<IActionResult> GeneratePrintJob(
            [FromBody] GeneratePrintJobRequest request,
            [FromHeader(Name = "X-Session-Token")] string? sessionToken)
        {
            try
            {
                // 1. Validate Request

                if (request.OrderId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Valid Order ID is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(sessionToken))
                {
                    return Unauthorized(new
                    {
                        message = "Customer session token is required."
                    });
                }

                // 2. Validate Customer Session

                string sessionTokenHash =
                    Convert.ToHexString(
                        SHA256.HashData(
                            Encoding.UTF8.GetBytes(sessionToken)
                        )
                    );

                var session = await _context.CustomerSessions
                    .FirstOrDefaultAsync(x =>
                        x.SessionTokenHash == sessionTokenHash &&
                        x.ExpiresAtUtc > DateTime.UtcNow &&
                        x.EndedAtUtc == null
                    );

                if (session == null)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid or expired customer session."
                    });
                }

                // 3. Find Order

                var order = await _context.Orders
                    .FirstOrDefaultAsync(x =>
                        x.OrderId == request.OrderId &&
                        x.TenantId == session.TenantId &&
                        x.ShopId == session.ShopId &&
                        x.SessionId == session.SessionId
                    );

                if (order == null)
                {
                    return NotFound(new
                    {
                        message = "Order not found for this customer session."
                    });
                }

                // 4. Check Order Status

                if (order.OrderStatus != "Paid")
                {
                    return BadRequest(new
                    {
                        message = "Order payment is not completed.",
                        orderStatus = order.OrderStatus
                    });
                }

                // 5. Verify Successful Payment

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(x =>
                        x.OrderId == order.OrderId &&
                        x.TenantId == order.TenantId &&
                        x.ShopId == order.ShopId &&
                        x.PaymentStatus == "Successful" &&
                        x.GatewaySignatureVerified == true
                    );

                if (payment == null)
                {
                    return BadRequest(new
                    {
                        message = "Successful verified payment not found."
                    });
                }

                // 6. Check Existing Print Job

                var existingJob = await _context.PrintJobs
                    .FirstOrDefaultAsync(x =>
                        x.OrderId == order.OrderId &&
                        x.TenantId == order.TenantId &&
                        x.ShopId == order.ShopId
                    );

                if (existingJob != null)
                {
                    return Ok(new
                    {
                        message = "Print job already exists.",
                        printJobId = existingJob.PrintJobId,
                        orderId = existingJob.OrderId,
                        jobGuid = existingJob.JobGuid,
                        jobStatus = existingJob.JobStatus
                    });
                }

                // 7. Check Uploaded Files

                var orderFiles = await _context.OrderFiles
                    .Where(x => x.OrderId == order.OrderId)
                    .ToListAsync();

                if (!orderFiles.Any())
                {
                    return BadRequest(new
                    {
                        message = "No uploaded documents found for this order."
                    });
                }

                // 8. Check Print Settings

                var orderFileIds = orderFiles
                    .Select(x => x.OrderFileId)
                    .ToList();

                var printSettings = await _context.PrintSettings
                    .Where(x => orderFileIds.Contains(x.OrderFileId))
                    .ToListAsync();

                if (printSettings.Count != orderFiles.Count)
                {
                    return BadRequest(new
                    {
                        message = "Print settings are missing for one or more documents."
                    });
                }

                // 9. Validate Printer Selection

                var printerIds = printSettings
                    .Where(x => x.PrinterId.HasValue)
                    .Select(x => x.PrinterId!.Value)
                    .Distinct()
                    .ToList();

                if (printerIds.Count > 1)
                {
                    return BadRequest(new
                    {
                        message = "Multiple printers in one order are not supported by the current Print Job structure."
                    });
                }

                long? printerId = printerIds.FirstOrDefault();

                // 10. Create Print Job

                var printJob = new PrintJob
                {
                    TenantId = order.TenantId,
                    ShopId = order.ShopId,
                    OrderId = order.OrderId,
                    PrinterId = printerId == 0 ? null : printerId,
                    PrintAgentId = null,
                    JobGuid = Guid.NewGuid(),
                    JobStatus = "Pending",
                    Priority = 5,
                    QueuedAtUtc = DateTime.UtcNow
                };

                _context.PrintJobs.Add(printJob);

                await _context.SaveChangesAsync();

                // 11. Create Print Log

                var printLog = new PrintLog
                {
                    PrintJobId = printJob.PrintJobId,
                    LogLevel = "Information",
                    Message = "Print job created successfully after payment verification.",
                    LoggedAtUtc = DateTime.UtcNow
                };

                _context.PrintLogs.Add(printLog);

                await _context.SaveChangesAsync();

                // 12. Return Response

                return Ok(new
                {
                    message = "Print job generated successfully.",

                    printJobId = printJob.PrintJobId,

                    jobGuid = printJob.JobGuid,

                    orderId = printJob.OrderId,

                    orderNumber = order.OrderNumber,

                    tenantId = printJob.TenantId,

                    shopId = printJob.ShopId,

                    printerId = printJob.PrinterId,

                    jobStatus = printJob.JobStatus,

                    priority = printJob.Priority,

                    queuedAtUtc = printJob.QueuedAtUtc
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while generating print job.",
                    error = ex.Message
                });
            }
        }
    }
}
