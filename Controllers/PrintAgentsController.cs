
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Models;
using Smart_Xerox_Centre.Security;
using Smart_Xerox_Centre.DTOs.PrintAgents;
using System.Security.Cryptography;
using System.Text;

namespace Smart_Xerox_Centre.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrintAgentsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly CurrentUserService _currentUser;

        public PrintAgentsController(
            ApplicationDbContext db,
            CurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        // POST: api/PrintAgents/register
        [HttpPost("register")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterPrintAgentRequest request)
        {
            try
            {
                if (!_currentUser.TenantId.HasValue)
                    return Unauthorized("Tenant information not found.");

                if (request.ShopId <= 0)
                    return BadRequest("Valid ShopId is required.");

                if (string.IsNullOrWhiteSpace(request.AgentName))
                    return BadRequest("AgentName is required.");

                long tenantId = _currentUser.TenantId.Value;

                // Verify Shop belongs to logged-in Tenant
                var shop = await _db.Shops.FirstOrDefaultAsync(x =>
                    x.ShopId == request.ShopId &&
                    x.TenantId == tenantId &&
                    x.Status == "Active");

                if (shop == null)
                    return NotFound("Active Shop not found in your Tenant.");

                // Generate secure random Agent Key
                byte[] keyBytes = RandomNumberGenerator.GetBytes(32);

                string agentKey = Convert.ToBase64String(keyBytes)
                    .TrimEnd('=')
                    .Replace('+', '-')
                    .Replace('/', '_');

                // Store only SHA256 hash
                string agentKeyHash = Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(agentKey)));

                var agent = new PrintAgent
                {
                    TenantId = tenantId,
                    ShopId = shop.ShopId,
                    AgentName = request.AgentName.Trim(),
                    MachineName = request.MachineName?.Trim(),
                    AgentVersion = request.AgentVersion?.Trim(),
                    AgentKeyHash = agentKeyHash,
                    Status = "Offline",
                    RegisteredAtUtc = DateTime.UtcNow
                };

                _db.PrintAgents.Add(agent);
                await _db.SaveChangesAsync();

                // Registration Log
                var agentLog = new PrintAgentLog
                {
                    PrintAgentId = agent.PrintAgentId,
                    LogLevel = "Information",
                    EventType = "Registration",
                    Message = "Print Agent registered successfully.",
                    LoggedAtUtc = DateTime.UtcNow
                };

                _db.PrintAgentLogs.Add(agentLog);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Print Agent registered successfully.",
                    printAgentId = agent.PrintAgentId,
                    tenantId = agent.TenantId,
                    shopId = agent.ShopId,
                    agentName = agent.AgentName,
                    machineName = agent.MachineName,
                    agentVersion = agent.AgentVersion,
                    status = agent.Status,

                    // Show this key only once.
                    agentKey = agentKey
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error while registering Print Agent.",
                    error = ex.Message
                });
            }
        }

        // POST: api/PrintAgents/heartbeat
        [HttpPost("heartbeat")]
        [AllowAnonymous]
        public async Task<IActionResult> Heartbeat(
            [FromHeader(Name = "X-Agent-Key")] string? agentKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(agentKey))
                    return Unauthorized("Agent Key is required.");

                // Generate SHA256 hash of received key
                string agentKeyHash = Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(agentKey)));

                var agent = await _db.PrintAgents.FirstOrDefaultAsync(x =>
                    x.AgentKeyHash == agentKeyHash);

                if (agent == null)
                    return Unauthorized("Invalid Agent Key.");

                if (agent.Status == "Disabled")
                    return StatusCode(403, "Print Agent is disabled.");

                bool wasOffline = agent.Status != "Online";

                agent.Status = "Online";
                agent.LastHeartbeatAtUtc = DateTime.UtcNow;

                // Log only when Agent comes Online
                if (wasOffline)
                {
                    var agentLog = new PrintAgentLog
                    {
                        PrintAgentId = agent.PrintAgentId,
                        LogLevel = "Information",
                        EventType = "Heartbeat",
                        Message = "Print Agent is Online.",
                        LoggedAtUtc = DateTime.UtcNow
                    };

                    _db.PrintAgentLogs.Add(agentLog);
                }

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Heartbeat received successfully.",
                    printAgentId = agent.PrintAgentId,
                    agentName = agent.AgentName,
                    status = agent.Status,
                    lastHeartbeatAtUtc = agent.LastHeartbeatAtUtc
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error while processing Agent Heartbeat.",
                    error = ex.Message
                });
            }
        }
    }
}
