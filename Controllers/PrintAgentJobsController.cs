
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Models;
using Smart_Xerox_Centre.DTOs.PrintAgentJobs;
using System.Security.Cryptography;
using System.Text;

namespace Smart_Xerox_Centre.Controllers
{
    [ApiController]
    [Route("api/print-agent-jobs")]
    public class PrintAgentJobsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public PrintAgentJobsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // POST: api/print-agent-jobs/fetch
        [HttpPost("fetch")]
        public async Task<IActionResult> FetchPendingJob(
            [FromHeader(Name = "X-Agent-Key")] string? agentKey)
        {
            if (string.IsNullOrWhiteSpace(agentKey))
                return Unauthorized("Agent Key is required.");

            string agentKeyHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(agentKey)));

            var agent = await _db.PrintAgents
                .FirstOrDefaultAsync(x => x.AgentKeyHash == agentKeyHash);

            if (agent == null)
                return Unauthorized("Invalid Agent Key.");

            if (agent.Status == "Disabled")
                return StatusCode(403, "Print Agent is disabled.");

            if (agent.Status != "Online")
                return StatusCode(403, "Print Agent is not Online.");

            // Do not assign another job while this agent has an active job.
            bool hasActiveJob = await _db.PrintJobs.AnyAsync(x =>
                x.PrintAgentId == agent.PrintAgentId &&
                (x.JobStatus == "Assigned to Agent" ||
                 x.JobStatus == "Agent Received" ||
                 x.JobStatus == "Printing" ||
                 x.JobStatus == "Submitted to Spooler"));

            if (hasActiveJob)
                return Conflict("Agent already has an active Print Job.");

            // Find next Pending Job from same Tenant and Shop.
            var job = await _db.PrintJobs
                .Where(x =>
                    x.TenantId == agent.TenantId &&
                    x.ShopId == agent.ShopId &&
                    x.JobStatus == "Pending" &&
                    (x.PrintAgentId == null ||
                     x.PrintAgentId == agent.PrintAgentId))
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.QueuedAtUtc)
                .FirstOrDefaultAsync();

            if (job == null)
            {
                return Ok(new
                {
                    message = "No Pending Print Job available.",
                    hasJob = false
                });
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                // Recheck and claim job inside transaction.
                var currentJob = await _db.PrintJobs
                    .FirstOrDefaultAsync(x =>
                        x.PrintJobId == job.PrintJobId &&
                        x.TenantId == agent.TenantId &&
                        x.ShopId == agent.ShopId &&
                        x.JobStatus == "Pending" &&
                        (x.PrintAgentId == null ||
                         x.PrintAgentId == agent.PrintAgentId));

                if (currentJob == null)
                {
                    await transaction.RollbackAsync();

                    return Conflict(
                        "Print Job was already assigned to another Agent.");
                }

                currentJob.PrintAgentId = agent.PrintAgentId;
                currentJob.JobStatus = "Assigned to Agent";

                var previousAttempts = await _db.PrintJobAttempts
                    .Where(x => x.PrintJobId == currentJob.PrintJobId)
                    .CountAsync();

                int attemptNumber = previousAttempts + 1;

                var attempt = new PrintJobAttempt
                {
                    PrintJobId = currentJob.PrintJobId,
                    AttemptNumber = attemptNumber,
                    PrintAgentId = agent.PrintAgentId,
                    StartedAtUtc = DateTime.UtcNow,
                    AttemptStatus = "Started"
                };

                _db.PrintJobAttempts.Add(attempt);

                var printLog = new PrintLog
                {
                    PrintJobId = currentJob.PrintJobId,
                    LogLevel = "Information",
                    Message = "Print Job assigned to Print Agent.",
                    LoggedAtUtc = DateTime.UtcNow
                };

                _db.PrintLogs.Add(printLog);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                var response = new PrintJobResponseDto
                {
                    PrintJobId = currentJob.PrintJobId,
                    JobGuid = currentJob.JobGuid,
                    OrderId = currentJob.OrderId,
                    TenantId = currentJob.TenantId,
                    ShopId = currentJob.ShopId,
                    PrinterId = currentJob.PrinterId,
                    Priority = currentJob.Priority,
                    JobStatus = currentJob.JobStatus,
                    AttemptNumber = attemptNumber,
                    QueuedAtUtc = currentJob.QueuedAtUtc
                };

                return Ok(new
                {
                    message = "Print Job assigned successfully.",
                    hasJob = true,
                    job = response
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message = "Error while assigning Print Job.",
                    error = ex.Message
                });
            }
        }


        // GET: api/print-agent-jobs/current
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentJob(
            [FromHeader(Name = "X-Agent-Key")] string? agentKey)
        {
            if (string.IsNullOrWhiteSpace(agentKey))
                return Unauthorized("Agent Key is required.");

            string agentKeyHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(agentKey)));

            var agent = await _db.PrintAgents
                .FirstOrDefaultAsync(x => x.AgentKeyHash == agentKeyHash);

            if (agent == null)
                return Unauthorized("Invalid Agent Key.");

            if (agent.Status == "Disabled")
                return StatusCode(403, "Print Agent is disabled.");

            if (agent.Status != "Online")
                return StatusCode(403, "Print Agent is not Online.");

            var job = await _db.PrintJobs
                .Where(x =>
                    x.PrintAgentId == agent.PrintAgentId &&
                    x.TenantId == agent.TenantId &&
                    x.ShopId == agent.ShopId &&
                    (x.JobStatus == "Assigned to Agent" ||
                     x.JobStatus == "Agent Received" ||
                     x.JobStatus == "Printing"))
                .OrderBy(x => x.QueuedAtUtc)
                .FirstOrDefaultAsync();

            if (job == null)
            {
                return Ok(new
                {
                    message = "No active Print Job found.",
                    hasJob = false
                });
            }

            var attempt = await _db.PrintJobAttempts
                .Where(x =>
                    x.PrintJobId == job.PrintJobId &&
                    x.PrintAgentId == agent.PrintAgentId)
                .OrderByDescending(x => x.AttemptNumber)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                message = "Current Print Job found.",
                hasJob = true,
                job = new PrintJobResponseDto
                {
                    PrintJobId = job.PrintJobId,
                    JobGuid = job.JobGuid,
                    OrderId = job.OrderId,
                    TenantId = job.TenantId,
                    ShopId = job.ShopId,
                    PrinterId = job.PrinterId,
                    Priority = job.Priority,
                    JobStatus = job.JobStatus,
                    AttemptNumber = attempt?.AttemptNumber ?? 0,
                    QueuedAtUtc = job.QueuedAtUtc
                }
            });
        }


        // GET: api/print-agent-jobs/{printJobId}/file
        [HttpGet("{printJobId:long}/file")]
        public async Task<IActionResult> DownloadPrintJobFile(
            long printJobId,
            [FromHeader(Name = "X-Agent-Key")] string? agentKey)
        {
            if (string.IsNullOrWhiteSpace(agentKey))
                return Unauthorized("Agent Key is required.");

            string agentKeyHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(agentKey)));

            var agent = await _db.PrintAgents
                .FirstOrDefaultAsync(x => x.AgentKeyHash == agentKeyHash);

            if (agent == null)
                return Unauthorized("Invalid Agent Key.");

            if (agent.Status == "Disabled")
                return StatusCode(403, "Print Agent is disabled.");

            var job = await _db.PrintJobs
                .FirstOrDefaultAsync(x =>
                    x.PrintJobId == printJobId &&
                    x.PrintAgentId == agent.PrintAgentId &&
                    x.TenantId == agent.TenantId &&
                    x.ShopId == agent.ShopId &&
                    (x.JobStatus == "Assigned to Agent" ||
                     x.JobStatus == "Agent Received" ||
                     x.JobStatus == "Printing"));

            if (job == null)
                return NotFound("Assigned Print Job not found.");

            var orderFile = await _db.OrderFiles
                .FirstOrDefaultAsync(x => x.OrderId == job.OrderId);

            if (orderFile == null)
                return NotFound("Order File not found.");

            //string filePath = orderFile.StoragePath;

            //if (!System.IO.Path.IsPathRooted(filePath))
            //{
            //    filePath = System.IO.Path.Combine(
            //        Directory.GetCurrentDirectory(),
            //        filePath);
            //}


            string filePath = orderFile.StoragePath;

            if (!System.IO.Path.IsPathRooted(filePath))
            {
                filePath = System.IO.Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "App_Data",
                    filePath);
            }

            filePath = System.IO.Path.GetFullPath(filePath);



            if (!System.IO.File.Exists(filePath))
                return NotFound("Stored document file not found.");

            string contentType = orderFile.ContentType;

            return PhysicalFile(
                filePath,
                contentType,
                orderFile.OriginalFileName);
        }

    }
}
