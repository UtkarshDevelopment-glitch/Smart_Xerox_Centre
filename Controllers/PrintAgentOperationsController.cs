using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Services;

namespace Smart_Xerox_Centre.Controllers;

[ApiController]
[Route("api/print-agents")]
public sealed class PrintAgentOperationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITokenHasher _hasher;

    public PrintAgentOperationsController(ApplicationDbContext db, ITokenHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    // Agent sends its raw secret in X-Agent-Key. Database stores only SHA-256 hash.
    [HttpPost("{printAgentId:long}/heartbeat")]
    public async Task<IActionResult> Heartbeat(long printAgentId, [FromHeader(Name = "X-Agent-Key")] string? agentKey,
        [FromBody] HeartbeatRequest request)
    {
        if (string.IsNullOrWhiteSpace(agentKey)) return Unauthorized();
        var agent = await _db.PrintAgents.FirstOrDefaultAsync(x => x.PrintAgentId == printAgentId);
        if (agent == null || !CryptographicEquals(agent.AgentKeyHash, _hasher.Hash(agentKey)))
            return Unauthorized();

        if (agent.Status == "Disabled" || agent.Status == "Inactive")
            return Forbid();

        agent.LastHeartbeatAtUtc = DateTime.UtcNow;
        agent.Status = "Online";
        if (!string.IsNullOrWhiteSpace(request.AgentVersion))
            agent.AgentVersion = request.AgentVersion;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Heartbeat received.", agent.PrintAgentId, agent.Status, agent.LastHeartbeatAtUtc });
    }

    private static bool CryptographicEquals(string a, string b)
    {
        try
        {
            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(a), Convert.FromHexString(b));
        }
        catch { return false; }
    }
}

public sealed class HeartbeatRequest
{
    public string? AgentVersion { get; set; }
    public string? MachineName { get; set; }
}
