
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using System.Security.Cryptography;
using System.Text;
using Smart_Xerox_Centre.DTOs.PrintAgentJobs;

namespace Smart_Xerox_Centre.Controllers
{
    [ApiController]
    [Route("api/print-agent-job-status")]
    public class PrintAgentJobStatusController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public PrintAgentJobStatusController(
            ApplicationDbContext db)
        {
            _db = db;
        }

[HttpPost("{printJobId:long}/update")]
public async Task<IActionResult> UpdateStatus(
    long printJobId,
    [FromBody]Smart_Xerox_Centre.DTOs.PrintAgentJobs.UpdatePrintJobStatusRequest request,
    [FromHeader(Name = "X-Agent-Key")] string? agentKey)
        {
            if (string.IsNullOrWhiteSpace(agentKey))
                return Unauthorized("Agent Key is required.");

            if (request.PrintJobId != printJobId)
                return BadRequest("Print Job ID mismatch.");

            string hash = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(agentKey)));

            var agent = await _db.PrintAgents
                .FirstOrDefaultAsync(x => x.AgentKeyHash == hash);

            if (agent == null)
                return Unauthorized("Invalid Agent Key.");

            if (agent.Status != "Online")
                return StatusCode(403, "Agent is not Online.");

            var job = await _db.PrintJobs
                .FirstOrDefaultAsync(x =>
                    x.PrintJobId == printJobId &&
                    x.PrintAgentId == agent.PrintAgentId &&
                    x.TenantId == agent.TenantId &&
                    x.ShopId == agent.ShopId);

            if (job == null)
                return NotFound("Print Job not found.");

            var attempt = await _db.PrintJobAttempts
                .Where(x =>
                    x.PrintJobId == printJobId &&
                    x.PrintAgentId == agent.PrintAgentId)
                .OrderByDescending(x => x.AttemptNumber)
                .FirstOrDefaultAsync();

            if (attempt == null)
                return NotFound("Print Job Attempt not found.");

            string newStatus = request.JobStatus;

            if (newStatus == "Agent Received")
            {
                if (job.JobStatus != "Assigned to Agent")
                    return BadRequest("Invalid status transition.");
            }
            else if (newStatus == "Printing")
            {
                if (job.JobStatus != "Agent Received")
                    return BadRequest("Invalid status transition.");

                job.StartedAtUtc = DateTime.UtcNow;
            }
            else if (newStatus == "Completed")
            {
                if (job.JobStatus != "Printing")
                    return BadRequest("Invalid status transition.");

                job.CompletedAtUtc = DateTime.UtcNow;
                attempt.EndedAtUtc = DateTime.UtcNow;
                attempt.AttemptStatus = "Completed";
            }
            else if (newStatus == "Failed")
            {
                if (job.JobStatus != "Printing" &&
                    job.JobStatus != "Agent Received")
                    return BadRequest("Invalid status transition.");

                attempt.EndedAtUtc = DateTime.UtcNow;
                attempt.AttemptStatus = "Failed";
                attempt.ErrorMessage = request.ErrorMessage;
            }
            else
            {
                return BadRequest("Unsupported Job Status.");
            }

            job.JobStatus = newStatus;

            if (newStatus == "Failed")
                job.LastError = request.ErrorMessage;

            _db.PrintLogs.Add(new Models.PrintLog
            {
                PrintJobId = printJobId,
                LogLevel = newStatus == "Failed"
                    ? "Error" : "Information",
                Message = "Mock Print Status Updated: " + newStatus,
                LoggedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Print Job Status Updated Successfully.",
                printJobId = job.PrintJobId,
                jobStatus = job.JobStatus,
                attemptStatus = attempt.AttemptStatus
            });
        }
    }
}
