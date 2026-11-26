
using System.Security.Claims;

namespace Smart_Xerox_Centre.Security;

public class CurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public long? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?
                .User.FindFirstValue(ClaimTypes.NameIdentifier);

            return long.TryParse(value, out var id) ? id : null;
        }
    }

    public long? TenantId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?
                .User.FindFirstValue("TenantId");

            return long.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
