
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Smart_Xerox_Centre.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoleTestController : ControllerBase
{
    [HttpGet("superadmin")]
    [Authorize(Roles = "SuperAdmin")]
    public IActionResult SuperAdmin()
    {
        return Ok("SuperAdmin Access Successful");
    }

    [HttpGet("shopowner")]
    [Authorize(Roles = "ShopOwner")]
    public IActionResult ShopOwner()
    {
        return Ok("ShopOwner Access Successful");
    }

    [HttpGet("shopstaff")]
    [Authorize(Roles = "ShopStaff")]
    public IActionResult ShopStaff()
    {
        return Ok("ShopStaff Access Successful");
    }
}
