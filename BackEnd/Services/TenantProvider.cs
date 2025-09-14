using System.Security.Claims;
public sealed class TenantProvider(IHttpContextAccessor accessor) : ITenantProvider
{
    public Guid TenantId =>
        Guid.TryParse(accessor.HttpContext?.User?.FindFirstValue("tenantId"), out var id)
            ? id : Guid.Empty;
}