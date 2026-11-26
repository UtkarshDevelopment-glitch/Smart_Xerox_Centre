using Microsoft.AspNetCore.Http;
using Smart_Xerox_Centre.Security;

namespace Smart_Xerox_Centre.Security;

public sealed class TenantAccess
{
    private readonly CurrentUserService _current;
    public TenantAccess(CurrentUserService current) => _current = current;

    public long RequireTenantId()
    {
        if (!_current.IsAuthenticated || !_current.TenantId.HasValue)
            throw new UnauthorizedAccessException("Authenticated tenant context is missing.");
        return _current.TenantId.Value;
    }

    public long RequireUserId()
    {
        if (!_current.IsAuthenticated || !_current.UserId.HasValue)
            throw new UnauthorizedAccessException("Authenticated user context is missing.");
        return _current.UserId.Value;
    }
}
