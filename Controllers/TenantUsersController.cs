
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Security;

namespace Smart_Xerox_Centre.Controllers;

[ApiController]
[Route("api/tenant-users")]
[Authorize(Roles = "ShopOwner")]
public class TenantUsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly CurrentUserService _currentUser;

    public TenantUsersController(
        ApplicationDbContext db,
        CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // GET: api/tenant-users
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        if (!_currentUser.TenantId.HasValue)
            return Unauthorized("Tenant information not found.");

        var tenantId = _currentUser.TenantId.Value;

        var users = await _db.Users
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => new
            {
                x.UserId,
                x.TenantId,
                x.FullName,
                x.Email,
                x.Phone,
                x.Status,
                x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(users);
    }
}
