using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Models;

namespace Smart_Xerox_Centre.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserRolesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public UserRolesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserRole>>> GetAll()
        => Ok(await _db.UserRoles.AsNoTracking().ToListAsync());

    [HttpGet("{userId:long}/{roleId:int}")]
    public async Task<ActionResult<UserRole>> Get(long userId, int roleId)
    {
        var item = await _db.UserRoles.FindAsync(userId, roleId);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<UserRole>> Create(UserRole item)
    {
        _db.UserRoles.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { userId = item.UserId, roleId = item.RoleId }, item);
    }

    [HttpDelete("{userId:long}/{roleId:int}")]
    public async Task<IActionResult> Delete(long userId, int roleId)
    {
        var item = await _db.UserRoles.FindAsync(userId, roleId);
        if (item == null) return NotFound();
        _db.UserRoles.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
