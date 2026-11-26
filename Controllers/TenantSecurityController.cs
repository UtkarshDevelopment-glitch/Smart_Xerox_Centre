
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_Xerox_Centre.Security;

namespace Smart_Xerox_Centre.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantSecurityController : ControllerBase
{
    private readonly CurrentUserService _currentUser;

    public TenantSecurityController(CurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet("my-tenant")]
    public IActionResult GetMyTenant()
    {
        if (!_currentUser.TenantId.HasValue)
            return Unauthorized("Tenant information not found.");

        return Ok(new
        {
            Message = "Tenant Security Working Successfully",
            UserId = _currentUser.UserId,
            TenantId = _currentUser.TenantId
        });
    }
}
